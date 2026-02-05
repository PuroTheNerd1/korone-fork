import {GroupWithShout} from "../../services/groups-typed";
import {createUseStyles} from "react-jss";
import GroupsPageStore from "./stores/GroupsPageStore";
import {useEffect} from "react";
import GroupsNew from "./index";
import AuthenticationStore from "../../stores/authentication";
import {useRouter} from "next/dist/client/router";
import { itemNameToEncodedName } from "../../services/catalog";

const useStyles = createUseStyles({
    spinnerContainer: {
        height: '85vh',
    },
});

// load all of the data required for the group page here
const GroupPreProcessor = ({group}: {group:GroupWithShout|null}) => {
    const s = useStyles();
    const store = GroupsPageStore.useContainer();
    const auth = AuthenticationStore.useContainer();
    const router = useRouter();

    useEffect(() => {
        let expectedUrl = `/groups/${group.id}/${itemNameToEncodedName(group.name)}`;
        if (typeof window !== 'undefined' && window.location.pathname !== expectedUrl) {
            router.replace(expectedUrl);
            return;
        }
        store.fetchData(group);
    }, [auth.isPending, group]);

    // TODO: if group does not exist, keep us on this page with the sidebar
    //  but show group not found with group layout or something like that
    if (group == null) return <div>noo not found</div>
    if (!store.group && store.isLoading) return <div className={`container ${s.spinnerContainer}`}>
        <span className="spinner" style={{ height: "100%", backgroundSize: "auto 36px" }}/>
    </div>

    return <GroupsNew />
}

export default GroupPreProcessor;