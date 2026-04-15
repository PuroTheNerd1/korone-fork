import {getInfo, GroupWithShout} from "../../services/groups-typed";
import GroupsPageStore from "./stores/GroupsPageStore";
import {useEffect} from "react";
import GroupsNew from "./index";
import AuthenticationStore from "../../stores/authentication";
import {useRouter} from "next/dist/client/router";
import {itemNameToEncodedName} from "../../services/catalog";
import UserGroupsStore from "./stores/UserGroupsStore";
import {wait} from "../../lib/utils";

// load all of the data required for the group page here
const GroupPreProcessor = ({group, loadDefault}: {group:GroupWithShout|null,loadDefault:boolean}) => {
    const store = GroupsPageStore.useContainer();
    const ustore = UserGroupsStore.useContainer();
    const auth = AuthenticationStore.useContainer();
    const router = useRouter();

    useEffect(async () => {
        let expectedUrl = `/groups/${group?.id}/${itemNameToEncodedName(group?.name)}`;
        if (typeof window !== 'undefined' && window.location.pathname !== expectedUrl && group !== null) {
            router.replace(expectedUrl);
            return;
        }

        if (loadDefault && !group) {
            while (auth.isPending) {
                await wait(0.1);
            }
            let ug = await ustore.fetchData();
            if (!ug || ug.length <= 0) {
                router.replace("/search/groups");
                return;
            }
            let defaultGroup = ug.find(g => g.isPrimary);
            if (!defaultGroup) defaultGroup = ug.shift();
            if (!defaultGroup) {
                router.replace("/search/groups");
                return;
            }

            group = await getInfo({groupId: defaultGroup.group.id});
        } else {
            ustore.fetchData();
        }
        if (group) store.fetchData(group, true);
    }, [auth.isPending, group]);

    return <GroupsNew />
}

export default GroupPreProcessor;