import React, {useEffect, useRef, useState} from "react";
import { createUseStyles } from "react-jss";
import OldVerticalTabs from "../oldVerticalTabs2";
import AuthenticationStore from "../../stores/authentication";
import Templates from "./subpages/templates";
import BasicSettings from "./subpages/basicSettings";
import Access from "./subpages/access";
import AdvancedSettings from "./subpages/advancedSettings";
import ActionButton from "../actionButton";
import useButtonStyles from "../../styles/buttonStyles";
import FeedbackStore from "../../stores/feedback";
import {FeedbackType} from "../../models/feedback";

const useStyles = createUseStyles({
    contentContainer: {
        padding: '15px',
    },
    creationContainer: {
        display: 'flex',
        flexDirection: 'column',
    },
    actionContainer: {
        display: 'flex',
        flexDirection: 'row',
        gap: '10px',
        marginTop: 20,
    },
    properPadding: {
        padding: '4px 8px'
    },
    WillTheRealSlimShadyPleaseStandUp: {
        '& *': {
            fontFamily: 'Source Sans Pro, Arial'
        }
    }
})

const CreateGame = props => {
    const auth = AuthenticationStore.useContainer();
    if (!auth.userId || !auth.username) return null;
    const s = useStyles();
    const feedback = FeedbackStore.useContainer();
    const but = useButtonStyles();
    const [tab, setTab] = useState('Templates');
    const [locked, setLocked] = useState(false);
    
    const [selectedTemplate, setSelectedTemplate] = useState(36568);
    const gameName = useRef('');
    const [gameDescription, setGameDescription] = useState('');
    const [commentsEnabled, setCommentsEnabled] = useState('true');
    const [gameGenre, setGameGenre] = useState('All');
    
    const [playableDevices, setPlayableDevices] = useState({
        computer: true,
        phone: true,
        tablet: true,
        console: false
    });
    const [playerCount, setPlayerCount] = useState(10);
    const [access, setAccess] = useState('Everyone');
    
    //const [gearGenres, setGearGenres] = useState(false);
    const [uncopylocked, setUncopylocked] = useState(false);
    
    const options = [
        {
            name: 'Templates',
            displayName: 'Templates',
            element: <Templates template={selectedTemplate} setTemplate={setSelectedTemplate} />,
        },
        {
            name: 'BasicSettings',
            displayName: 'Basic Settings',
            element: <BasicSettings default={`${auth.username}'s Place Number: {NumHere}`} gameName={gameName} gameDescription={gameDescription} setGameDescription={setGameDescription} commentsEnabled={commentsEnabled} setCommentsEnabled={setCommentsEnabled} gameGenre={gameGenre} setGameGenre={setGameGenre} />,
        },
        {
            name: 'Access',
            displayName: 'Access',
            element: <Access playableDevices={playableDevices} setPlayableDevices={setPlayableDevices} playerCount={playerCount} setPlayerCount={setPlayerCount} access={access} setAccess={setAccess} />,
        },
        {
            name: 'Advanced Settings',
            displayName: 'Advanced Settings',
            element: <AdvancedSettings uncopylocked={uncopylocked} setUncopylocked={setUncopylocked} />,
        }
    ]
    
    return <div className={`container ssp ${s.WillTheRealSlimShadyPleaseStandUp}`}>
        <h1 style={{ fontWeight: 600, marginBottom: '10px' }}>Create Game</h1>
        <div>
            <OldVerticalTabs contentStyles={`${s.contentContainer} vTabContent`} options={options} default={tab} onChange={n => setTab(n.name)}/>
        </div>
        <div className={s.actionContainer}>
            <ActionButton className={s.properPadding} buttonStyle={but.buyButton} label='Create Game' onClick={e => {
                e.preventDefault();
                if (locked) return;
                setLocked(true);
                
                if (gameName.current.value < 3 || gameName.current.value > 100) {
                }
                if (gameDescription && gameDescription.length > 1000) {}
                
                const gameStuff = {
                    template: selectedTemplate,
                    name: gameName.current.value,
                    description: gameDescription,
                    genre: gameGenre,
                    comments: commentsEnabled === 'true',
                    playableDevices,
                    maxPlayerCount: playerCount,
                    placeAccess: access,
                    uncopylocked
                };
                console.log(gameStuff);
                feedback.addFeedback("Game successfully created!", FeedbackType.SUCCESS);
                setTimeout(() => window.location.href = '/develop', 5000);
            }} />
            <ActionButton className={s.properPadding} buttonStyle={but.cancelButton} label='Cancel' onClick={e => {
                e.preventDefault();
                window.location.href = '/develop?View=0';
            }} />
        </div>
    </div>
}

export default CreateGame;