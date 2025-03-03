import React, { useEffect, useRef, useState } from "react";
import { createUseStyles } from "react-jss";
import { getBaseUrl, getFullUrl } from "../../../../lib/request";
import { getCreatedItems, uploadAsset } from "../../../../services/develop";
import AuthenticationStore from "../../../../stores/authentication";
import ActionButton from "../../../actionButton";
import AssetList from "../assetList";
import FeedbackStore from "../../../../stores/feedback";
import {useSearchParams} from "react-router-dom";
import {getUniverseGamePasses, getUserGames} from "../../../../services/games";

const useStyles = createUseStyles({
    subtext: {
        color: '#d2d2d2',
        fontSize: '14px',
        marginLeft: '8px',
    },
    inputItemName: {
        width: 'calc(100% - 200px)',
        //marginLeft: '28px',
    },
    inputItemDesc: {
        width: 'calc(100% - 200px)',
        marginLeft: '28px',
    },
    gameSelectContainer: {
        marginTop: 30,
        display: 'flex',
        justifyContent: 'space-between',
        '& h2': {
            display: 'inline-block',
        }
    },
    selectFrom: {},
    gameSelector: {},
})

const GamePasses = props => {
    const { id, groupId } = props;

    const auth = AuthenticationStore.useContainer();

    //const [feedback, feedback.addFeedback] = useState(null);
    const feedback = FeedbackStore.useContainer();
    const [locked, setLocked] = useState(false);
    const [previewing, setPreviewing] = useState(false);
    // 0 == loading, 1 == failed, array = success
    const [gamesList, setGamesList] = useState(0);
    const [selectedGame, setSelectedGame] = useState(null); // should be a ref to an entry in the games list
    // 0 == loading, 1 == failed, array = success
    const [passesList, setPassesList] = useState(0);
    const [searchParams, setSearchParams] = useSearchParams();
    const nameRef = useRef(null);
    const descRef = useRef(null);
    //const [gameLocked, setGameLocked] = useState(false);
    /**
     * @type {React.Ref<HTMLInputElement>}
     */
    const fileRef = useRef(null);

    const onSubmit = e => {
        e.preventDefault();
        if (locked) return;
        if (!fileRef.current.files.length) return feedback.addFeedback('You must select a file');
        if (!nameRef.current.value) return feedback.addFeedback('You must specify a name');
        if (!descRef.current.value) return feedback.addFeedback('You must specify a description');
        let image = fileRef.current.files[0];
        if (image.size >= 8e+7) return feedback.addFeedback('The file is too large');
        if (image.size === 0) return feedback.addFeedback('The file is empty');

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
            feedback.addFeedback(e.message);
            setLocked(false);
        })
    }
    
    useEffect(() => {
        if (Array.isArray(gamesList) && gamesList[searchParams.get("universeId")] !== null) {
            setSelectedGame(gamesList[searchParams.get("universeId")]);
        } else {
            searchParams.delete("universeId");
        }
    }, [searchParams]);
    
    useEffect(() => {
        setGamesList(null); // might cause issues with rerendering
        if (!auth.userId || !groupId) return;
        
        try {
            setSelectedGame(null);
            getUserGames({ userId: auth.userId }).then(setGamesList);
        } catch (e) {
            feedback.addFeedback(e);
            setGamesList(1);
        }
        
    }, [auth.userId, id, groupId]);
    
    useEffect(() => {
        try {
            getUniverseGamePasses({
                // limit: 100,
                // cursor: '',
                universeId: selectedGame.id,
                //groupId,
            }).then(setPassesList);
        } catch (e) {
            feedback.addFeedback(e);
            setPassesList(1);
        }
    }, [gamesList])

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
                <p>Find your image: <input ref={fileRef} type='file' /> {
                    //{feedback && <span className='text-danger'>{feedback}</span>}
                }</p>
                <p>Game Pass Name: <input ref={nameRef} type='text' className={s.inputItemName} /></p>
                <p>Description: <input ref={descRef} type='text' className={s.inputItemDesc} /></p>
                <div className='float-left'>
                    <ActionButton disabled={locked} label='Preview' onClick={onSubmit} />
                </div>
            </div>
        </div>
        <div className={`${s.gameSelectContainer} col-12`}>
            <div>
                <h2>
                    Game Passes
                </h2>
            </div>
            {
                Array.isArray(gamesList) && gamesList.length > 0 && <span className={`${s.selectFrom}`}>
                Select from Public Games:
                <select className={`${s.gameSelector}`} onChange={e => {
                    setSelectedGame(parseInt(e.target.value));
                }}>
                    {
                        gamesList.map((game) /** @type {UserGameEntry} */ =>
                            <option value={game} key={game.id} label={game.name} />
                        )
                    }
                </select>
            </span>
            }
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