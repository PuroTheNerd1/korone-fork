import {createContainer} from "unstated-next";
import {useEffect, useRef, useState} from "react";
import FeedbackStore from "../../../stores/feedback";
import {FeedbackType} from "../../../models/feedback";
import {multiGetUserThumbnails} from "../../../services/thumbnails";
import AuthenticationStore from "../../../stores/authentication";
import {getMyAvatar, getRules, redrawMyAvatar, setColors, setRigType, setScales} from "../../../services/avatar";
import * as AvatarService from "../../../services/avatar";
import {wait} from "../../../lib/utils";

const AvatarInfoStore = createContainer(() => {
    // CURRENT STUFF
    const [wearingAssets, setWearingAssets] = useState(null);
    const [bodyColors, setBodyColors] = useState(null);
    const [bodyScales, setBodyScales] = useState(null);
    const [bodyRigType, setBodyRigType] = useState(null);
    const [avThumb, setAvThumb] = useState(null);
    
    const [isRendering, setIsRendering] = useState(false);
    const [avRules, setAvRules] = useState(false);
    const [loadingAvatar, setLoadingAvatar] = useState(true);
    const [canForce, setCanForce] = useState(true);
    
    // changed assetId is number of which asset id has been changed
    // denoted by whether its positive or negative integer
    const [modifiedAsset, setModifiedAsset] = useState(null);
    // the rest of these should correspond to the actual value name in the API to get avatars
    // so modified scaling would be { height: 0.5 }
    const [modifiedBC, setModifiedBC] = useState(null);
    const [modifiedScaling, setModifiedScaling] = useState(null);
    const [modifiedRigType, setModifiedRigType] = useState(null);
    
    const debo = useRef(false);

    const feedback = FeedbackStore.useContainer();
    const auth = AuthenticationStore.useContainer();
    
    function AddAsset(asset) {
        setModifiedAsset(asset);
    }
    
    function RemoveAsset(asset) {
        setModifiedAsset({
            ...asset,
            assetId: asset.assetId * -1,
        });
    }
    
    async function ReloadAvatar(){
        setLoadingAvatar(true);
        setAvThumb(null);
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
        setLoadingAvatar(false);
        setIsRendering(true);
    }
    
    async function ForceRender() {
        if (!canForce) return;
        setCanForce(false);
        
        await redrawMyAvatar();
        setAvThumb(null);
        await wait(1);
        setIsRendering(true);
        await wait(3);
        setCanForce(true);
    }
    
    async function GetUpdatedAvatar() {
        if (isRendering) {
            while (isRendering) {
                await wait(1);
            }
        }
        setAvThumb(null);
        await wait(1);
        setIsRendering(true);
    }
    
    useEffect(ReloadAvatar, []);
    
    useEffect(async () => {
        if (debo.current || !isRendering || avThumb != null) return;
        debo.current = true;
        
        let attempts = 0;
        while (avThumb == null && attempts <= 10) {
            let thumbnail = await multiGetUserThumbnails({userIds: [auth.userId]})
                .then(result => result[0]);
            if (thumbnail.state === "Completed" && typeof thumbnail.imageUrl === "string") {
                setAvThumb(thumbnail.imageUrl);
                break;
            } else {
                console.warn("User thumbnail has not completed rendering yet.");
            }
            attempts++;
            await wait(1);
        }
        if (attempts > 10 && avThumb == null)
            feedback.addFeedback("Could not get new avatar render. Please try again later.", FeedbackType.ERROR);
        setIsRendering(false);
        await wait(0.5);
        debo.current = false;
    }, [isRendering]);
    
    useEffect(() => {
        if (!modifiedScaling) return;
        
        const applyScaling = async () => {
            setBodyScales(prev => {
                const newScales = { ...prev, ...modifiedScaling };
                setModifiedScaling(null);
                (async () => {
                    await setScales(newScales);
                    await GetUpdatedAvatar();
                })();
                return newScales;
            });
        };
        
        applyScaling().then();
    }, [modifiedScaling]);
    useEffect(() => {
        if (!modifiedBC) return;
        
        const applyBC = async () => {
            setBodyColors(prev => {
                const newBC = { ...prev, ...modifiedBC };
                setModifiedBC(null);
                (async () => {
                    await setColors(newBC);
                    await GetUpdatedAvatar();
                })();
                return newBC;
            });
        };
        
        applyBC().then();
    }, [modifiedBC]);
    useEffect(() => {
        if (!modifiedRigType) return;
        let newRigType = modifiedRigType;
        setModifiedRigType(null);
        
        const applyRigType = async () => {
            setBodyRigType(newRigType);
            await setRigType(newRigType);
            await GetUpdatedAvatar();
        };
        
        applyRigType().then();
    }, [modifiedRigType]);
    useEffect(() => {
        if (!modifiedAsset) return;
        let newAsset = modifiedAsset;
        setModifiedAsset(null);
        
        if (wearingAssets.length >= 15) {
            feedback.addFeedback("Too many assets equipped", FeedbackType.ERROR);
            return;
        }
        
        setWearingAssets(prev => {
            let updated;
            if (IsNegative(newAsset.assetId)) {
                updated = prev.filter(v => v.assetId !== newAsset.assetId * -1);
            } else {
                updated = [...prev, newAsset];
            }
            
            (async () => {
                await AvatarService.setWearingAssets({ assetIds: updated.map(d => d.assetId) });
                await GetUpdatedAvatar();
            })();
            
            return updated;
        });
    }, [modifiedAsset]);
    
    return {
        // Functions
        AddAsset,
        RemoveAsset,
        ForceRender,
        GetUpdatedAvatar,
        ReloadAvatar,
        
        // States (all United)
        wearingAssets,
        
        bodyColors,
        setBodyColors,
        
        bodyScales,
        setBodyScales,
        
        bodyRigType,
        setBodyRigType,
        
        setModifiedRigType,
        setModifiedBC,
        setModifiedScaling,
        
        avThumb,
        setAvThumb,
        
        loadingAvatar,
        setLoadingAvatar,
        
        isRendering,
        /**
         * @type AvatarRules
         */
        avRules,
    }
})

function IsNegative(int) {
    return int < 0;
}

export default AvatarInfoStore;
