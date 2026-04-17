import React, { useEffect, useState } from "react";
import { createUseStyles } from "react-jss";
import AuthenticationStore from "../../stores/authentication";
import NavigationStore from "../../stores/navigation";
import LinkEntry from "./components/linkEntry";
import { avPageStyleType, getAvPageStyle, getTheme, themeType } from "../../services/theme";
import {
    getPendingApplicationCount,
    getPendingAssets, getPendingGroupIcons,
    getPendingIcons,
    getPendingReportCount
} from "../../services/admin";
import PlayerHeadshot from "../playerHeadshot";
import Link from "../link";

const useNavSideBarStyles = createUseStyles({
    container: {
        position: 'fixed',
        top: 0,
        left: 0,
        zIndex: 999,
    },
    card: {
        width: '175px',
        background: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? '#393939' : 'var(--white-color)',
        color: 'var(--text-color-primary)',
        height: '100vh',
        paddingLeft: '10px',
        paddingRight: '10px',
        marginTop: '12px',
        boxShadow: '0 0 3px rgba(25, 25, 25, 0.3)',
        paddingTop: '40px',
        '@media(max-width: 991px)': {
            paddingTop: '75px',
        }
    },
    divider: {
        borderBottom: '1px solid var(--text-color-secondary)',
        borderColor: p => p.theme === themeType.dark || p.theme === themeType.obc2019 ? 'rgba(255, 255, 255, 0.2)' : 'var(--text-color-secondary)',
        height: '2px',
        width: '100%',
        marginTop: '5px',
        marginBottom: '8px'
    },
    upgradeNowButton: {
        marginTop: '10px',
        background: 'var(--primary-color)',
        fontSize: '15px',
        fontWeight: 500,
        width: '100%',
        paddingTop: '8px',
        paddingBottom: '8px',
        textAlign: 'center',
        color: 'white',
        borderRadius: '4px',
        '&:hover': {
            background: 'var(--primary-color-hover)',
        },
    },
    egghuntlogo: {
        display: 'block',
        marginTop: '10px',
        width: '100%',
        cursor: 'pointer',
        '& img': {
            width: '100%',
            borderRadius: '4px',
        },
    },
    usernameContainer: {
        display: 'flex',
        alignItems: 'center',
        cursor: 'pointer',
    },
    username: {
        fontSize: '16px',
        fontWeight: '500',
        margin: 0,
        padding: 0,
        color: p => p.theme === themeType.obc2019 ? 'var(--white-color)' : 'var(--text-color-primary)',
        textDecoration: 'none'
    },
    userIconContainer: {
        fontSize: "16px",
        width: "1.4em",
        borderRadius: "50%",
        aspectRatio: '1 / 1',
        backgroundColor: 'var(--text-color-secondary)',
        overflow: 'hidden',
        marginRight: 3,
        boxShadow: "0 1px 4px 0 rgba(25,25,25,0.3)",
        transition: "box-shadow 200ms ease",
        "&:hover": {
            boxShadow: "0 1px 6px 0 rgba(25,25,25,0.75)",
        }
    },
    userIcon: {},
});

const NavSideBar = props => {
    const authStore = AuthenticationStore.useContainer();
    const navStore = NavigationStore.useContainer();
    const mainNavBarRef = props.mainNavBarRef;
    const [dimensions, setDimensions] = useState({
        height: window.innerHeight,
        width: window.innerWidth
    })
    const s = useNavSideBarStyles({ theme: getTheme() });
    const [pendingCount, setPendingCount] = useState(69);
    useEffect(() => {
        window.addEventListener('resize', () => {
            setDimensions({
                height: window.innerHeight,
                width: window.innerWidth
            });
        });
        
        const setPending = async () => {
            if (authStore.isStaff) {
                let pendingAssCount = 0;
                // im pretty sure there's a better way to do this while keeping everything asynchronous but ong i forgot 😭😭
                getPendingAssets().then(d => {
                    pendingAssCount += d.length;
                    setPendingCount(pendingAssCount);
                });
                getPendingIcons().then(d => {
                    pendingAssCount += d.length
                    setPendingCount(pendingAssCount);
                });
                getPendingApplicationCount().then(d => {
                    pendingAssCount += d.count
                    setPendingCount(pendingAssCount);
                });
                getPendingReportCount().then(d => {
                    pendingAssCount += d.count
                    setPendingCount(pendingAssCount);
                });
                getPendingGroupIcons().then(d => {
                    pendingAssCount += d.length
                    setPendingCount(pendingAssCount);
                });
            }
        }
        setPending().then();
    }, []);
    useEffect(() => {
        if (pendingCount === 0) setPendingCount(69);
    }, [pendingCount]);
    const paddingTop = mainNavBarRef.current && mainNavBarRef.current.clientHeight + 'px' || 0;
    
    if (navStore.isSidebarOpen === false && dimensions.width <= 1324) {
        return null;
    }
    
    const isStaff = authStore.isStaff;
    
    return <div className={s.container}>
        <div className={s.card}>
            <Link href={`/users/${authStore.userId}/profile`}>
                <a href={`/users/${authStore.userId}/profile`}>
                    <div className={s.usernameContainer}>
                        <div className={s.userIconContainer}>
                            <PlayerHeadshot id={authStore.userId} name={authStore.username} className={s.userIcon}/>
                        </div>
                        <a className={s.username}>{authStore.username}</a>
                    </div>
                </a>
            </Link>
            <div className={s.divider}/>
            <LinkEntry theme={getTheme()} name='Home' url='/home' icon='icon-nav-home'/>
            <LinkEntry theme={getTheme()} name='Profile' url={'/users/' + authStore.userId + '/profile'}
                       icon='icon-nav-profile'/>
            <LinkEntry theme={getTheme()} name='Messages' url='/My/Messages' icon='icon-nav-message'
                       count={authStore.notificationCount.messages}/>
            <LinkEntry theme={getTheme()} name='Friends' url={'/users/' + authStore.userId + '/friends'}
                       icon='icon-nav-friends' count={authStore.notificationCount.friendRequests}/>
            <LinkEntry theme={getTheme()} name='Avatar'
                       url={getAvPageStyle() === avPageStyleType.Legacy ? '/My/Character.aspx' : '/My/Avatar'}
                       icon='icon-nav-charactercustomizer'/>
            <LinkEntry theme={getTheme()} name='Inventory' url={'/users/' + authStore.userId + '/inventory'}
                       icon='icon-nav-inventory'/>
            <LinkEntry theme={getTheme()} name='Trade' url='/My/Trades.aspx' icon='icon-nav-trade'
                       count={authStore.notificationCount.trades}/>
            <LinkEntry theme={getTheme()} name='Groups' url='/My/Groups.aspx' icon='icon-nav-group'/>
            <LinkEntry theme={getTheme()} name='Forums' url='/Forum/Default.aspx' icon='icon-nav-forum'/>
            <LinkEntry theme={getTheme()} name='Download' url='/download' icon='icon-nav-download'/>
            <LinkEntry theme={getTheme()} name='Egg Hunt' url='/EggHunt2026' icon='icon-nav-egg'/>
            {isStaff ? (
                <LinkEntry theme={getTheme()} name='Panel' url='/admin' icon='icon-edit' count={pendingCount}/>
            ) : null}
            <a href='/BuildersClub/Upgrade.ashx'><p className={s.upgradeNowButton}>Upgrade Now</p></a>
            <Link href='/EggHunt2026'>
                <a href='/EggHunt2026' className={s.egghuntlogo}>
                    <img src='/egghunt2026/secondarylogo.png' alt='Egg Hunt 2026'/>
                </a>
            </Link>
        </div>
    </div>
}

export default NavSideBar;