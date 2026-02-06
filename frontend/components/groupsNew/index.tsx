import {createUseStyles} from "react-jss";
import GroupsPageStore from "./stores/GroupsPageStore";
import {useEffect} from "react";
import GroupIcon from "../groupIcon";
import { ThumbnailFromState } from "../AvatarEditorPage/components/avatarCardList";
import CreatorLink from "../creatorLink";
import HorizontalTabs from "../horizontalTabs";
import AboutTab from "./components/AboutTab";
import AuthenticationStore from "../../stores/authentication";
import StoreTab from "./components/StoreTab";
import AffiliatesTab from "./components/AffiliatesTab";

const useStyles = createUseStyles({});

const GroupsPage = () => {
    const s = useStyles();
    const store = GroupsPageStore.useContainer();
    const auth = AuthenticationStore.useContainer();
    const { group,posts,members,userGroups } = GroupsPageStore.useContainer();

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

    return <div className={`container`}>
        <div></div>
        <div className={`${s.groupDetailWrapper}`}>
            <div className={`${s.groupHeaderContainer} section-content noShadow`}>
                <div className={`${s.groupImage}`}>
                    <GroupIcon name={group.name} url={ThumbnailFromState(group.icon.imageUrl, group.icon.state)} />
                </div>
                <div className={`${s.groupInfoContainer}`}>
                    <h1>{group.name}</h1>
                    <div className={`${s.groupOwner}`}>
                        <span>By</span>
                        <CreatorLink type={'User'} id={group.owner.userId} name={group.owner.displayName} />
                    </div>
                    <div className={`${s.groupInfo}`}>
                        <div className={`${s.groupInfoMembers}`}>
                            <span>Members</span>
                            <h3>{group.memberCount}</h3>
                        </div>
                    </div>
                </div>
            </div>
            {/*only show if signed in*/}
            {
                auth.isAuthenticated ? <div className={`${s.shoutContainer}`}>
                </div> : null
            }
            <HorizontalTabs
                options={[
                    {name: "About", element: <AboutTab />},
                    {name: "Store", element: <StoreTab />},
                    {name: "Affiliates", element: <AffiliatesTab />},
                ]}
                parentClass={`${s.tabContainer}`}
            />
            {
                auth.isAuthenticated ? <div className={`${s.wallContainer}`}>
                </div> : null
            }
        </div>
    </div>
}

export default GroupsPage;