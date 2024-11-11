import React, { useEffect, useRef, useState } from "react";
import { createUseStyles } from "react-jss";
import { getBaseUrl, getFullUrl } from "../../../../lib/request";
import { getCreatedItems, uploadAsset } from "../../../../services/develop";
import AuthenticationStore from "../../../../stores/authentication";
import ActionButton from "../../../actionButton";
import AssetList from "../assetList";

const useStyles = createUseStyles({
    subtext: {
        color: '#d2d2d2',
        fontSize: '14px',
        marginLeft: '8px',
    },
    inputItemName: {
        width: 'calc(100% - 200px)',
        marginLeft: '28px',
    },
    inputItemDesc: {
        width: 'calc(100% - 200px)',
        marginLeft: '28px',
    },
})

const GamePasses = props => {
    const { id, groupId } = props;

    const auth = AuthenticationStore.useContainer();

    const [feedback, setFeedback] = useState(null);
    const [locked, setLocked] = useState(false);
    const [previewing, setPreviewing] = useState(false);
    const [passesList, setPassesList] = useState(null);
    const [gamesList, setGamesList] = useState(null);
    const [selectedGame, setSelectedGame] = useState(null); // should be a ref to an entry in the games list
    const nameRef = useRef(null);
    const descRef = useRef(null);
    /**
     * @type {React.Ref<HTMLInputElement>}
     */
    const fileRef = useRef(null);

    const onSubmit = e => {
        e.preventDefault();
        if (locked) return;
        if (!fileRef.current.files.length) return setFeedback('You must select a file');
        if (!nameRef.current.value) return setFeedback('You must specify a name');
        if (!descRef.current.value) return setFeedback('You must specify a description');
        let image = fileRef.current.files[0];
        if (image.size >= 8e+7) return setFeedback('The file is too large');
        if (image.size === 0) return setFeedback('The file is empty');

        setLocked(true);
        uploadAsset({
            name: nameRef.current.value,
            assetTypeId: id,
            file: image,
            groupId,
            description: descRef.current.value
        }).then(() => {
            window.location.reload();
        }).catch(e => {
            setFeedback(e.message);
            setLocked(false);
        })
    }

    useEffect(() => {
        setPassesList(null);
        if (!auth.userId && !groupId) return;
        getCreatedItems({
            limit: 100,
            cursor: '',
            assetType: id,
            groupId,
        }).then(d => {
            setPassesList(d);
        });
    }, [auth.userId, id, groupId]);

    const s = useStyles();

    return <div className='row'>
        <div className='col-12'>
            <h2>
                Create a Game Pass
            </h2>
        </div>
        <div className='col-12'>
            <div className='ms-4 me-4 mt-4'>
                {//details.templateUrl ? <p>Did you use the template? If not, <a href={details.templateUrl}>download it here</a>.</p> : null}
                }
                <p>Target Game: </p>
                <p>Find your image: <input ref={fileRef} type='file' /> {feedback && <span className='text-danger'>{feedback}</span>}</p>
                <p>Game Pass Name: <input ref={nameRef} type='text' className={s.inputItemName} /></p>
                <p>Description: <input ref={descRef} type='text' className={s.inputItemDesc} /></p>
                <div className='float-left'>
                    <ActionButton disabled={locked} label='Preview' onClick={onSubmit} />
                </div>
            </div>
        </div>
        <div className='col-12 mt-4'>
            {passesList ? (
                passesList.data.length === 0 ?
                    <p>No Game Passes found.</p>
                    : <AssetList assets={passesList.data} />
            ) : null}
        </div>
    </div>
}

export default GamePasses;