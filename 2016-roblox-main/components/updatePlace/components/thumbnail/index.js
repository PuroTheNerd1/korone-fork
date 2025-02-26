import { createUseStyles } from 'react-jss';
import { useState, useEffect } from 'react';
import ActionCalls from './actionCalls';
import updatePlaceStore from "../../stores/updatePlaceStore";
import FeedbackStore from "../../../../stores/feedback";
import {multiGetAssetThumbnails} from "../../../../services/thumbnails";
import {getGameUrl} from "../../../../services/games";
import ActionButton from "../../../actionButton";
import buyButton from "../../../catalogDetailsPage/components/buyButton";
import useButtonStyles from "../../../../styles/buttonStyles";

const useStyles = createUseStyles({
    contentContainer: {
        display: 'flex',
        flexDirection: 'column',
        paddingLeft: '12px',
        paddingRight: '12px',
        marginTop: '24px',
    },
    header: {
        '& h3': {
            fontWeight: '400!important',
            marginBottom: '1.5rem!important',
            fontSize: '2rem',
            lineHeight: '1.2',
        },
    },
    mainContainer: {
        flex: '0 0 auto',
        display: 'flex',
        flexDirection: 'row',
    },
    iconContainer: {
        display: 'flex',
        flexDirection: 'column',
        borderRight: '1px solid var(--text-color-secondary)',
        paddingRight: '20px',
        aspectRatio: '1.25/1'
    },
    gameIcon: {
        width: '100%!important',
        display: 'block!important',
        verticalAlign: 'middle',
        aspectRatio: '16/9',
    },
    noteText: {
        marginTop: '5px',
        marginBottom: '20px',
        fontSize: '10px',
        fontWeight: '500',
        lineHeight: '1.4em',
        display: 'block',
        width: '100%',
        fontStyle: 'italic',
        color: '#d2d2d2'
    },
    callsToAction: {
        display: 'flex',
        flexDirection: 'column',
        marginLeft: '20px',
        '& p': {
            marginBottom: '10px'
        }
    },
    feedback: {
        padding: '15px',
        backgroundColor: '#E2EEFE',
        border: '1px solid #6586A3',
        fontSize: '16px',
        fontWeight: '400',
        lineHeight: '1.4em',
    },
    footerContainer: {
        flex: '0 0 auto',
    },
    normal: {
        padding: '3px 18px'
    },
})

function blobToBase64(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}

function getSrcFromNum(num) {
    switch (num) {
        case 0:
            return "/img/loading.png";
        case 1:
        case 3:
            return "/img/placeholder.png";
        case 2:
            return "/img/error.png";
        default:
            return num;
    }
}

const Thumbnail = props => {
    const s = useStyles();
    // 0 == loading, 1 == placeholder (for some reason), 2 == failed to load, 3 == pending, string = success
    const [thumbnail, setThumbnail] = useState(0);
    const store = updatePlaceStore.useContainer();
    const feedback = FeedbackStore.useContainer();
    const buttonStyles = useButtonStyles();
    
    const refreshIcon = () => {
        setThumbnail(0);
        multiGetAssetThumbnails({ assetIds: [store.placeId] }).then(thumbs => {
            try {
                if (thumbs.length === 0) {
                    setThumbnail(1);
                    return;
                }
                if (!Array.isArray(thumbs) || typeof thumbs[0].imageUrl !== 'string') {
                    throw new Error('Thumbnail did not load properly! Setting Thumbnail to 2...');
                }
                setThumbnail(thumbs[0].state === 'Pending' ? "/img/placeholder.png" : thumbs[0].state === 'Blocked' ? "/img/blocked.png" : thumbs[0].imageUrl);
            } catch (e) {
                setThumbnail(2);
                feedback.addFeedback(e);
            }
        });
    }
    
    // TODO: will later have to be rewritten to support multiple thumbnails
    useEffect(refreshIcon, [store.placeId, store.details.universeId]);
    
    return <div className={s.contentContainer}>
        <div className={`${s.header} col-12`}>
            <h3>Thumbnails</h3>
        </div>
        <div className={`${s.mainContainer} col-12`}>
            <div className={`${s.iconContainer} col-8`}>
                <img className={s.gameIcon} src={getSrcFromNum(thumbnail)}  alt='Game Thumbnail'/>
                <p className={s.noteText}>Note: You can only have 1 thumbnail per game (for now).</p>
            </div>
            <div className={`${s.callsToAction} col-4`}>
                <p style={{
                    fontSize: '18px',
                }}>Change the Thumbnail</p>
                <p style={{
                    fontSize: '16px'
                }}>Media type:</p>
                <ActionCalls placeId={
                    store.placeId
                } feedback={feedback} refreshIcon={refreshIcon} />
            </div>
        </div>
        <div className={`${s.footerContainer} col-12`}>
            <div className='d-inline-block'>
                <ActionButton disabled={store.locked} buttonStyle={buttonStyles.continueButton} className={s.normal} label='Save'
                              onClick={() => {
                                  window.location.href = getGameUrl({placeId: store.placeId, name: 'placeholder'})
                              }}/>
            </div>
            <div className='d-inline-block ms-4'>
                <ActionButton disabled={store.locked} buttonStyle={buttonStyles.cancelButton} className={s.normal} label='Cancel'
                              onClick={() => {
                                  window.location.href = getGameUrl({placeId: store.placeId, name: 'placeholder'})
                              }}/>
            </div>
        </div>
    </div>
};

export default Thumbnail;