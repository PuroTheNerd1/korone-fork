import { createUseStyles } from "react-jss"
import CatalogDetailsPage from "../stores/catalogDetailsPage";
import { useEffect, useState } from "react";
import { getAudioURL } from "../../../services/catalog";

const useStyles = createUseStyles({
    wrapper: {
        //overflowX: 'hidden',
        position: 'relative',
        width: '100%',
        marginTop: 'auto',
    },
    img: {
        bottom: 0,
        right: 0,
        margin: '6px',
        position: 'absolute',
        //marginRight: '0',
    },
    audioPlayButton: {
        width: '48px',
        height: '48px',
        backgroundSize: '96px auto',
        cursor: 'pointer',
    },
});

const PlayButton = props => {
    const s = useStyles();
    const store = CatalogDetailsPage.useContainer();
    const [audio, setAudio] = useState(null);
    const [playing, setPlaying] = useState(false);
    const [audioUrl, setAudioUrl] = useState(null);

    useEffect(() => {
        getAudioURL({ audioId: store.details.id }).then(audioUrl => {
            setAudioUrl(audioUrl);
        });
    })

    useEffect(() => {
        if (playing && audio) {
            const playAudio = () => audio.play();
            const setPlayingFalse = () => setPlaying(false);
            audio.addEventListener('canplaythrough', playAudio);
            audio.addEventListener('ended', setPlayingFalse);
            return () => {
                audio.removeEventListener('canplaythrough', playAudio);
                audio.removeEventListener('ended', setPlayingFalse);
            };
        } else if (audio) {
            audio.pause();
            audio.currentTime = 0;
            setAudio(null);
        }
    }, [playing, audio]);

    const handlePlayPause = () => {
        if (!playing && typeof (audioUrl) == 'string') {
            const newAudio = new Audio(audioUrl);
            setAudio(newAudio);
            setPlaying(true);
        } else {
            setPlaying(false);
        }
    };

    return <div className={s.wrapper}>
        <span className={`${playing ? 'icon-pause-big' : 'icon-play-big'} ${s.img} ${s.audioPlayButton}`}
            onClick={handlePlayPause}>
        </span>
    </div>
}

export default PlayButton;