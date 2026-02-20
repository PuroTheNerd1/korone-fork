import dayjs from "dayjs";
import { useState, useRef } from "react";
import { createUseStyles } from "react-jss";
import { getGameUrl } from "../../../services/games";
import { multiGetUniverseIcons2 } from "../../../services/thumbnails";
import Activity from "../../userActivity";
import ChatStore from "../../chat/chatStore";
import DashboardStore from "../stores/dashboardStore";
import Link from "../../link";
import PlayerHeadshot from "../../playerHeadshot";

const useStyles = createUseStyles({
    friendEntry: {
        padding: 0,
        maxWidth: '100px',
        overflow: 'hidden',
        listStyle: 'none',
        width: '11.11111%',
        minWidth: '95px',
        '@media (max-width: 991px)': {
            width: '14.28571%'
        },
        '@media (max-width: 767px)': {
            width: '20%'
        },
        '@media (max-width: 543px)': {
            width: '33.33333%'
        },
    },
    friendWrapper: {
        margin: '3px auto',
    },
    thumbnailWrapper: {
        maxWidth: '90px',
        borderRadius: '100%',
        border: 'none',
        boxShadow: '0 1px 4px 0 rgba(25,25,25,0.3)',
        margin: '0 auto',
        display: 'block',
        width: '100%',
        position: 'relative',
        aspectRatio: '1 / 1',
        '&:hover': {
            transition: 'box-shadow 200ms ease',
            boxShadow: '0 1px 6px 0 rgba(25,25,25,0.75)'
        }
    },
    username: {
        whiteSpace: 'nowrap',
        textOverflow: 'ellipsis',
        overflow: 'hidden',
        textAlign: 'center',
        marginBottom: 0,
        width: '100%',
        marginTop: '3px',
        fontSize: '15px',
        fontWeight: 300,
        color: 'var(--text-color-primary)',
        '&:hover': {
            textDecoration: 'none!important',
            color: 'var(--primary-color)'
        },
    },
    activityWrapper: {
        float: 'right',
        zIndex: 2,
        position: 'absolute',
        right: 0,
        bottom: 0,
    },
    img: {
        borderRadius: '100%',
    },
    popup: {
        position: 'fixed',
        zIndex: 9999,
        minWidth: '220px',
        background: 'var(--white-color)',
        borderRadius: '6px',
        boxShadow: '0 4px 16px rgba(25,25,25,0.2)',
        overflow: 'hidden',
        border: '1px solid rgba(25,25,25,0.08)',
    },
    gameSection: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        padding: '10px 12px',
        borderBottom: '1px solid rgba(25,25,25,0.08)',
    },
    gameThumbnailImg: {
        width: '72px',
        height: '72px',
        objectFit: 'cover',
        borderRadius: '4px',
        flexShrink: 0,
        background: '#e3e3e3',
    },
    gameThumbnailPlaceholder: {
        width: '72px',
        height: '72px',
        borderRadius: '4px',
        flexShrink: 0,
        background: '#e3e3e3',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
    },
    gameInfo: {
        display: 'flex',
        flexDirection: 'column',
        paddingLeft: '10px',
        justifyContent: 'center',
        flex: 1,
        overflow: 'hidden',
    },
    gameLocation: {
        fontSize: '11px',
        color: 'var(--text-color-secondary)',
        marginBottom: '2px',
        fontWeight: 400,
    },
    gameName: {
        fontWeight: 600,
        fontSize: '13px',
        color: 'var(--text-color-primary)',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        marginBottom: '8px',
    },
    joinButton: {
        display: 'inline-block',
        background: '#00b06f',
        color: '#fff',
        borderRadius: '4px',
        padding: '4px 14px',
        fontWeight: 600,
        fontSize: '13px',
        textDecoration: 'none',
        '&:hover': {
            background: '#009960',
            color: '#fff',
            textDecoration: 'none',
        }
    },
    popupActions: {
        padding: '4px 0',
    },
    popupAction: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        padding: '8px 12px',
        color: 'var(--text-color-primary)',
        fontSize: '13px',
        cursor: 'pointer',
        textDecoration: 'none',
        '&:hover': {
            background: 'rgba(25,25,25,0.05)',
            textDecoration: 'none',
            color: 'var(--text-color-primary)',
        }
    },
    popupIcon: {
        marginRight: '10px',
        width: '20px',
        textAlign: 'center',
        flexShrink: 0,
    },
});

const FriendEntry = props => {
    const store = DashboardStore.useContainer();
    const chatStore = ChatStore.useContainer();
    const onlineStatus = store.friendStatus && store.friendStatus[props.id];
    const s = useStyles();
    const liRef = useRef(null);
    const [isHovered, setIsHovered] = useState(false);
    const [popupPos, setPopupPos] = useState({ top: 0, left: 0 });
    const [gameThumbnail, setGameThumbnail] = useState(null);

    const isOnline = onlineStatus && dayjs(onlineStatus.lastOnline).isAfter(dayjs().subtract(5, 'minutes'));
    const isPlaying = onlineStatus?.lastLocation === 'Playing';

    const handleMouseEnter = () => {
        if (liRef.current) {
            const rect = liRef.current.getBoundingClientRect();
            setPopupPos({
                top: rect.bottom + 8,
                left: rect.left,
            });
        }
        setIsHovered(true);
        if (isPlaying && !gameThumbnail && onlineStatus?.gameId) {
            multiGetUniverseIcons2({ universeIds: [onlineStatus.gameId] }).then(icons => {
                if (icons && icons[0]) {
                    setGameThumbnail(icons[0].imageUrl);
                }
            });
        }
    };

    const openChat = (e) => {
        e.preventDefault();
        let existing = [...chatStore.selectedConversation.filter(v => {
            if (v.conversationId === null) {
                return v.user.id !== props.id;
            }
            return true;
        })];
        if (existing.length >= 3) {
            existing = existing.slice(0, 2);
        }
        existing.unshift({ user: { id: props.id, username: props.name, isTyping: false }, conversationId: null, latestMessage: null });
        chatStore.setSelectedConversation(existing);
        setIsHovered(false);
    };

    return <li className={s.friendEntry} ref={liRef} onMouseEnter={handleMouseEnter} onMouseLeave={() => setIsHovered(false)}>
        <div className={s.friendWrapper}>
            <span>
                <Link href={`/users/${props.id}/profile`}>
                    <a>
                        <div className={s.thumbnailWrapper}>
                            <PlayerHeadshot className={s.img} id={props.id} name={props.name} />
                            {onlineStatus && <div className={s.activityWrapper}><Activity {...onlineStatus} /></div>}
                        </div>
                        <p className={s.username}>{props.name}</p>
                    </a>
                </Link>
            </span>
        </div>
        {isHovered && (
            <div className={s.popup} style={{ top: popupPos.top, left: popupPos.left }}>
                {isPlaying && isOnline && (
                    <div className={s.gameSection}>
                        {gameThumbnail
                            ? <img className={s.gameThumbnailImg} src={gameThumbnail} alt={onlineStatus.lastLocation} />
                            : <div className={s.gameThumbnailPlaceholder}><span className='icon-game' /></div>
                        }
                        <div className={s.gameInfo}>
                            <span className={s.gameLocation}>Playing</span>
                            <span className={s.gameName}>{onlineStatus.lastLocation}</span>
                            <Link href={getGameUrl({ placeId: onlineStatus.placeId, name: onlineStatus.lastLocation })}>
                                <a className={s.joinButton}>Join</a>
                            </Link>
                        </div>
                    </div>
                )}
                <div className={s.popupActions}>
                    <a className={s.popupAction} href='#' onClick={openChat}>
                        <span className={`${s.popupIcon} avatar-status icon-chat`} />
                        Chat with {props.name}
                    </a>
                    <Link href={`/users/${props.id}/profile`}>
                        <a className={s.popupAction}>
                            <span className={`${s.popupIcon} icon-menu-profile`} />
                            View Profile
                        </a>
                    </Link>
                </div>
            </div>
        )}
    </li>
}

export default FriendEntry;
