import {createUseStyles} from "react-jss";
import GroupsPageStore from "./stores/GroupsPageStore";
import {useEffect, useRef} from "react";
import GroupIcon from "../groupIcon";
import { ThumbnailFromState } from "../AvatarEditorPage/components/avatarCardList";
import CreatorLink from "../creatorLink";
import HorizontalTabs from "../horizontalTabs";
import AboutTab from "./components/AboutTab";
import StoreTab from "./components/StoreTab";
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
import GamesTab from "./components/GamesTab";
import UserGroupsStore from "./stores/UserGroupsStore";
import AuthenticationStore from "../../stores/authentication";
import AdBanner from "../ad/adBanner";

const useStyles = createUseStyles({
    groupDetailWrapper: {
        paddingLeft: 15,
        width: 'calc(100% - 15px - 160px)',
    },
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
        alignItems: 'center',
        width: '100%',
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
    joinGroupBtnContainer: {
        marginLeft: 'auto',
    },
    joinGroupBtn: {
        padding: 9,
        fontWeight: 500,
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
        display: 'inline-block',
        width: 48,
        height: 48,
        borderRadius: '50%',
        padding: 0,
        boxShadow: 'rgba(25, 25, 25, 0.3) 0px 1px 4px 0px',
        transition: 'box-shadow 200ms ease-in-out',
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
    wallPostContainer: {
        marginBottom: 12,
    },
    wallPostBtnContainer: {
        alignContent: 'end',
    },
    wallPostBtn: {
        padding: '9px 24px',
        fontSize: 16,
        marginLeft: 15,
    },
    wallPostText: {
        flexGrow: 1,
        borderRadius: 3,
        minHeight: 100,
        lineHeight: '1.6em',
        border: '1px solid var(--text-color-secondary)',
        padding: '5px 12px',
        fontSize: 16,
        fontWeight: 400,
        '-webkit-appearance': 'none',
        paddingRight: 30,
        '&:focus': {
            boxShadow: 'none',
            borderColor: 'var(--primary-color)',
            outline: 0,
        }
    },

    userGroupsWrapper: {
        width: 160,
    },
    userGroupsContainer: {},
    createGroupBtn: {
        padding: 9,
        fontSize: 18,
        fontWeight: 500,
        lineHeight: '100%',
        borderRadius: 3,
        width: '100%',
    },
    groupContainer: {
        padding: '10px 12px',
        position: 'relative',
        '&:hover': {
            boxShadow: 'inset 4px 0 0 0 var(--primary-color)',
            backgroundColor: 'var(--white-color-hover)',
        },
        '&.current': {
            boxShadow: 'inset 4px 0 0 0 var(--primary-color)',
        },
        '& span.primary': {
            top: 0,
            right: 0,
            width: 0,
            height: 0,
            position: 'absolute',
            borderTop: '20px solid var(--primary-color)',
            borderLeft: '20px solid transparent',
        },
    },
    groupNameContainer: {
        width: 'calc(100% - 37px)',
        '& span': {
            fontSize: 12,
            fontWeight: 500,
            overflow: 'hidden',
            width: '100%',
            display: 'inline-block',
        },
    },
    userGroupImage: {
        width: 32,
        height: 32,
        display: 'inline-block',
    },
    header: {
        '& h3': {
            fontSize: 32,
            fontWeight: 800,
            lineHeight: '100%',
        }
    },
    spinnerContainer: {
        height: '85vh',
    },
    banner: {
        marginBottom: 18,
    },
});

const GroupsPage = () => {
    const s = useStyles();
    const auth = AuthenticationStore.useContainer();
    const buttonStyles = useButtonStyles();
    const store = GroupsPageStore.useContainer();
    const { group,posts,members,userPerms } = GroupsPageStore.useContainer();
    const { userGroups } = UserGroupsStore.useContainer();

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
        <AdBanner className={s.banner} />
        <Section header="Groups" headerContainer={s.header} headerCenter={true} contentSectioned={false} headerChildren={<>
                <NewLink href={`/search/groups`}>
                    <span className={`link2018 fw-500`}>More Groups</span>
                </NewLink>
        </>} className={`flex`}>
            {
                !auth.isPending && auth.isAuthenticated ?
                    <div className={`${s.userGroupsWrapper} flex flex-column`}>
                        <ul className={`${s.userGroupsContainer} section-content noShadow padding-none flex flex-column flex-nowrap overflow-x-hidden overflow-y-auto w-100`}>
                            {
                                userGroups.map(group => {
                                    return <NewLink href={`/groups/${group.group.id}/${encodeURIComponent(group.group.name)}`}
                                                    className={`${s.groupContainer} flex ${group.group.id === store.group.id ? "current" : ""}`}
                                    >
                                        <div className={`${s.userGroupImage}`}>
                                            <GroupIcon url={ThumbnailFromState(group?.imageUrl, group?.state)} id={group.group.id} />
                                        </div>
                                        <div className={`${s.groupNameContainer} padding-l-5 overflow-hidden text-start align-content-center`}>
                                            <span className={`text-overflow text-start`}>{group.group.name}</span>
                                        </div>
                                        {
                                            group?.isPrimary ? <span className='primary' /> : null
                                        }
                                    </NewLink>
                                })
                            }
                        </ul>
                        <NewLink href='/My/CreateGroup.aspx'>
                            <ActionButton
                                label="Create Group"
                                buttonStyle={buttonStyles.newContinueButton}
                                className={s.createGroupBtn}
                            />
                        </NewLink>
                    </div> : null
            }
            {
                !store.group && store.isLoading ? <div className={`container ${s.spinnerContainer}`}>
                    <span className="spinner" style={{ height: "100%", backgroundSize: "auto 36px" }}/>
                </div> :
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
                                    {userPerms && userPerms.role.id > 1 ? <div className={`${s.groupStat}`}>
                                        <span>Rank</span>
                                        <h3 title={userPerms.role.description}>{userPerms.role.name}</h3>
                                    </div> : <ActionButton
                                        className={s.joinGroupBtn}
                                        divClassName={s.joinGroupBtnContainer}
                                        buttonStyle={buttonStyles.newContinueButton}
                                        label='Join Group'
                                        onClick={() => {
                                            console.log("joining group!!")
                                        }}
                                    />}
                                </div>
                            </div>
                            <div className={`${s.groupHeaderDropdownContainer}`}>
                                <GroupDropdown />
                            </div>
                        </div>
                        <HorizontalTabs
                            options={[
                                {name: "About", element: <AboutTab />},
                                {name: "Games", element: <GamesTab />},
                                {name: "Store", element: <StoreTab />},
                                // {name: "Affiliates", element: <AffiliatesTab />},
                            ]}
                            elementClass={`${s.tabContainer}`}
                        />
                        {
                            userPerms && userPerms.permissions.groupPostsPermissions.viewWall ? <Section header="Wall">
                                {posts.posts.length > 0 ? <div className={`${s.wallContainer} section-content noShadow`}>
                                    {userPerms.permissions.groupPostsPermissions.postToWall ? <div className={`${s.wallPostContainer} flex`}>
                            <textarea
                                className={s.wallPostText}
                                placeholder="Say something..."
                                maxLength={1000}
                                ref={textAreaRef}
                            />
                                        <ActionButton
                                            label="Post"
                                            className={s.wallPostBtn}
                                            divClassName={s.wallPostBtnContainer}
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
            }
        </Section>
    </div>
}

const GroupWallPost = (post: GroupPostEntry) => {
    const s = useStyles();
    const store = GroupsPageStore.useContainer();
    // TODO: add dropdown stuff

    const DropdownOptions: DropdownOption[] = [
        {
            name: 'Report Abuse',
            url: '/internal/report-abuse',
        },
    ]

    return <div className={`${s.postContainer} flex`}>
        <NewLink className={`${s.postHeadshotContainer}`} href={'/users/' + post.poster.user.userId + '/profile'}>
            <PlayerHeadshot id={post.poster.user.userId} name={post.poster.user.username}/>
        </NewLink>
        <div className={`${s.postInfo} flex flex-column align-items-start`}>
            <CreatorLink type={2} id={post.poster.user.userId} name={post.poster.user.displayName} />
            <span className={s.postBody}>{post.body}</span>
            <div>{post.poster.role.name} | {dayjs(post.updated).format('MMM D, YYYY | h:mm A')}</div>
        </div>
        <div className={`${s.groupHeaderDropdownContainer} flex align`}>
            <Dropdown2016 options={DropdownOptions} />
        </div>
    </div>
}

export default GroupsPage;