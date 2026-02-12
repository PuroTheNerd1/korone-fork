import {createUseStyles} from "react-jss";
import GroupsPageStore from "./stores/GroupsPageStore";
import {useEffect, useRef} from "react";
import GroupIcon from "../groupIcon";
import { ThumbnailFromState } from "../AvatarEditorPage/components/avatarCardList";
import CreatorLink from "../creatorLink";
import HorizontalTabs from "../horizontalTabs";
import AboutTab from "./components/AboutTab";
import AuthenticationStore from "../../stores/authentication";
import StoreTab from "./components/StoreTab";
import AffiliatesTab from "./components/AffiliatesTab";
import { abbreviateNumber } from "../../lib/numberUtils";
import GroupDropdown from "./components/GroupDropdown";
import ActionButton from "../actionButton";
import useButtonStyles from "../../styles/buttonStyles";
import Section from "./components/Section";
import {GroupPostEntry} from "../../services/groups-typed";
import Dropdown2016, { DropdownOption } from "../dropdown2016";
import NewLink from "../NewLink";
import dayjs from "../../lib/dayjs";
import PlayerHeadshot from "../playerHeadshot";

const useStyles = createUseStyles({
    groupDetailWrapper: {},
    groupHeaderContainer: {
        position: 'relative',
    },
    groupImage: {
        width: 128,
        aspectRatio: '1 / 1',
        marginRight: 12,
    },
    groupInfoContainer: {
        width: 'calc(100% - 140px)',
        '& h1': {
            fontWeight: 800,
            lineHeight: '1em',
            fontSize: 32,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
            maxWidth: '100%',
            padding: '5px 0',
            margin: 0,
        },
    },
    groupOwner: {
        margin: '5px 0',
        '& span': {
            color: 'var(--text-color-secondary)',
            marginRight: 5,
        },
        '& *': {
            fontWeight: 500,
        },
    },
    groupStatsContainer: {
        marginTop: 'auto',
    },
    groupStat: {
        padding: '0 12px',
        '& span': {
            color: 'var(--text-color-secondary)',
            fontWeight: 500,
            fontSize: 12,
        },
        '& h3': {
            fontSize: 12,
            fontWeight: 400,
            lineHeight: '1em',
            margin: '5px 0',
        },
    },
    groupHeaderDropdownContainer: {
        position: 'absolute',
        display: 'flex',
        top: 0,
        right: 6,
    },
    joinGroupBtn: {
        padding: 9,
        fontWeight: 500,
        marginBottom: 6,
    },
    tabContainer: {
        marginTop: 15,
    },
    wallContainer: {
        '& > div:not(:first-child)': {
            borderTop: '1px solid var(--background-color)',
        }
    },

    postContainer: {
        position: 'relative',
        padding: '12px 0',
        width: '100%',
        marginBottom: 0,
    },
    postHeadshotContainer: {
        width: 48,
        height: 48,
        borderRadius: '50%',
        padding: 0,
        boxShadow: 'rgba(25, 25, 25, 0.3) 0px 1px 4px 0px',
        overflow: 'hidden',
        backgroundColor: 'var(--text-color-secondary)',
        '&:hover': {
            boxShadow: 'rgba(25, 25, 25, 0.75) 0px 1px 6px 0px',
        },
    },
    postInfo: {
        marginLeft: 12,
        '& div': {
            marginTop: 12,
            width: '100%',
            color: 'var(--text-color-secondary)',
            lineHeight: '1.4em',
            fontSize: 12,
            fontWeight: 400,
            textAlign: 'start',
        },
    },
    postBody: {
        marginTop: 6,
        marginBottom: 0,
        color: "var(--text-color-primary)",
        fontSize: 16,
        wordWrap: "break-word",
        fontFamily: "HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif",
        fontWeight: 400,
        lineHeight: "1.4em",
        whiteSpace: "pre-wrap",
        textRendering: "auto",
        textAlign: 'start',
    },
});

const GroupsPage = () => {
    const s = useStyles();
    const buttonStyles = useButtonStyles();
    const store = GroupsPageStore.useContainer();
    const auth = AuthenticationStore.useContainer();
    const { group,posts,members,userGroups,userPerms } = GroupsPageStore.useContainer();

    const textAreaRef = useRef(null);

    useEffect(() => {
        console.log("GROUP HAS CHANGED:")
        console.dir(group);
        console.log(" ")
    }, [group]);

    useEffect(() => {
        console.log("POSTS HAVE CHANGED:")
        console.dir(posts);
        console.log(" ")
    }, [posts]);

    useEffect(() => {
        console.log("MEMBERS HAVE CHANGED:")
        console.dir(members);
        console.log(" ")
    }, [members]);

    useEffect(() => {
        console.log("ISLOADING HAS CHANGED:")
        console.dir(store.isLoading);
        console.log(" ")
    }, [store.isLoading]);

    useEffect(() => {
        console.log("USER GROUPS HAVE CHANGED:")
        console.dir(userGroups);
        console.log(" ")
    }, [userGroups]);

    if (group == null) return <div>noo not found</div>

    return <div className={`container padding-none`}>
        <div></div>
        <div className={`${s.groupDetailWrapper} flex flex-column`}>
            <div className={`${s.groupHeaderContainer} section-content noShadow flex`}>
                <div className={`${s.groupImage}`}>
                    <GroupIcon name={group.name} url={ThumbnailFromState(group.icon.imageUrl, group.icon.state)} />
                </div>
                <div className={`${s.groupInfoContainer} flex flex-column align-items-start`}>
                    <h1>{group.name}</h1>
                    <div className={`${s.groupOwner} flex align-items-center`}>
                        <span>By</span>
                        <CreatorLink type={'User'} id={group.owner.userId} name={group.owner.displayName} />
                    </div>
                    <div className={`${s.groupStatsContainer} flex`}>
                        <div className={`${s.groupStat}`}>
                            <span>Members</span>
                            <h3 title={group.memberCount.toString()}>{abbreviateNumber(group.memberCount)}</h3>
                        </div>
                        {userPerms && userPerms.role.id !== 1 ? <div className={`${s.groupStat}`}>
                            <span>Rank</span>
                            <h3 title={userPerms.role.description}>{userPerms.role.name}</h3>
                        </div> : null}
                        {!userPerms || userPerms.role.id <= 1 ? <ActionButton
                            className={s.joinGroupBtn}
                            buttonStyle={buttonStyles.newContinueButton}
                            label='Join Group'
                            onClick={() => {
                                console.log("joining group!!")
                            }}
                        /> : null}
                    </div>
                </div>
                <div className={`${s.groupHeaderDropdownContainer}`}>
                    <GroupDropdown />
                </div>
            </div>
            <HorizontalTabs
                options={[
                    {name: "About", element: <AboutTab />},
                    {name: "Store", element: <StoreTab />},
                    {name: "Affiliates", element: <AffiliatesTab />},
                ]}
                parentClass={`${s.tabContainer}`}
            />
            {
                userPerms && userPerms.permissions.groupPostsPermissions.viewWall ? <Section header="Wall">
                    {posts.posts.length > 0 ? <div className={`${s.wallContainer} section-content noShadow`}>
                        {userPerms.permissions.groupPostsPermissions.postToWall ? <div className={s.wallPostContainer}>
                            <input
                                className={s.wallPostText}
                                placeholder="Say something..."
                                maxLength={1000}
                                ref={textAreaRef}
                            />
                            <ActionButton
                                label="Post"
                                className={s.wallPostBtn}
                                buttonStyle={buttonStyles.newContinueButton}
                                onClick={() => {
                                    console.log("POSTING!!");
                                }}
                            />
                        </div> : null}
                        {posts.posts.map(post => (
                            <GroupWallPost key={post.id} {...post} />
                        ))}
                    </div> : <div className={`section-content-off noShadow`}>
                        Nobody has said anything yet...
                    </div>}
                </Section> : null
            }
        </div>
    </div>
}

const GroupWallPost = (post: GroupPostEntry) => {
    const s = useStyles();
    const store = GroupsPageStore.useContainer();

    const DropdownOptions: DropdownOption[] = [
        {
            name: 'Report Abuse',
            url: '/internal/report-abuse',
        },
    ]

    return <div className={`${s.postContainer}`}>
        <NewLink className={`${s.postHeadshotContainer}`} href={'/users/' + post.poster.userId + '/profile'}>
            <PlayerHeadshot id={post.poster.userId} name={post.poster.username}/>
        </NewLink>
        <div className={`${s.postInfo} flex flex-column align-items-start`}>
            <CreatorLink type={'User'} id={post.poster.userId} name={post.poster.displayName} />
            <span className={s.postBody}>{post.body}</span>
            <div>{dayjs(post.updated).format('MMM D, YYYY | h:mm A')}</div>
        </div>
        <div className={`${s.groupHeaderDropdownContainer} flex align`}>
            <Dropdown2016 options={DropdownOptions} />
        </div>
    </div>
}

export default GroupsPage;