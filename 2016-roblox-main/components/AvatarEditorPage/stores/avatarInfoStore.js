import {createContainer} from "unstated-next";
import {useEffect, useState} from "react";
import FeedbackStore from "../../../stores/feedback";
import {FeedbackType} from "../../../models/feedback";
import {multiGetUserThumbnails} from "../../../services/thumbnails";
import AuthenticationStore from "../../../stores/authentication";
import {getMyAvatar, getRules, redrawMyAvatar} from "../../../services/avatar";
import {wait} from "../../../lib/utils";

const AvatarInfoStore = createContainer(() => {
    // CURRENT STUFF
    const [wearingAssets, setWearingAssets] = useState(null);
    const [bodyColors, setBodyColors] = useState(null);
    const [bodyScales, setBodyScales] = useState(null);
    const [bodyRigType, setBodyRigType] = useState(null);
    const [avThumb, setAvThumb] = useState(null);
    
    // changed assets is an array of asset ids that are added or removed,
    // denoted by whether its positive or negative integer
    const [changedAssets, setChangedAssets] = useState([]);
    const [isRendering, setIsRendering] = useState(false);
    const [avRules, setAvRules] = useState(false);
    const [canForce, setCanForce] = useState(true);
    const [isModified, setIsModified] = useState(false);
    
    const feedback = FeedbackStore.useContainer();
    const auth = AuthenticationStore.useContainer();
    
    // could and probably should be merged into 1 function
    function AddAsset(assetId) {
        // check if asset was already marked for removal, if was, remove the negative and return
        // else, add to changedAssets array
        
        if (changedAssets.includes(assetId * -1)) {
            setChangedAssets(arr => arr.filter((_, index) => index !== assetId * -1));
            return;
        }
        
        setChangedAssets(arr => [...arr, assetId]);
    }
    
    function RemoveAsset(assetId) {
        // check if asset was already marked for addition, if was, remove the positive and return
        // else, add to changedAssets array
        
        if (changedAssets.includes(assetId)) {
            setChangedAssets(arr => arr.filter((_, index) => index !== assetId));
            return;
        }
        
        setChangedAssets(arr => [...arr, assetId * -1]);
    }
    
    async function ForceRender() {
        if (!canForce) return;
        setCanForce(false);
        
        await redrawMyAvatar();
        setAvThumb(null);
        setIsRendering(true);
        await wait(3);
        setCanForce(true);
    }
    
    useEffect(async () => {
        setAvRules(await getRules());
        let avatar = await getMyAvatar();
        setWearingAssets(avatar.assets.map(v => {
            return {
                name: v.name,
                assetId: v.id,
                assetType: v.assetType.id,
                assetTypeName: v.assetType.name
            }
        }));
        setBodyColors(avatar.bodyColors);
        setBodyRigType(avatar.playerAvatarType);
        setBodyScales(avatar.scales);
        setIsRendering(true);
    }, []);
    
    useEffect(async () => {
        // can cause issues if not done right
        if (!isRendering || avThumb != null) return;
        
        setIsModified(false);
        let attempts = 0;
        while (avThumb == null && attempts <= 10) {
            let thumbnail = await multiGetUserThumbnails({userIds: [auth.userId]})
                .then(result => result[0]);
            if (thumbnail.state === "Completed" && typeof thumbnail.imageUrl === "string") {
                setAvThumb(thumbnail.imageUrl);
            } else {
                console.warn("User thumbnail has not completed rendering yet.");
            }
            attempts++;
            await wait(1);
        }
        if (attempts > 10 && avThumb == null)
            feedback.addFeedback("Could not get new avatar render. Please try again later.", FeedbackType.ERROR);
        setIsRendering(false);
    }, [isRendering]);
    // this probably needs to be rewritten, just for testing for now
    useEffect(async () => {
        if (!isModified) return;
        await ForceRender();
    }, [isModified]);
    
    return {
        // Functions
        AddAsset,
        RemoveAsset,
        ForceRender,
        
        // States (all United)
        wearingAssets,
        
        bodyColors,
        setBodyColors,
        
        bodyScales,
        setBodyScales,
        
        bodyRigType,
        setBodyRigType,
        
        avThumb,
        setAvThumb,
        
        isRendering,
        /**
         * @type AvatarRules
         */
        avRules,
        isModified,
    }
})

export default AvatarInfoStore;
