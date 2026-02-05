import {createContainer} from "unstated-next";
import {useState} from "react";
import {
    getMembers, getUserGroupsV2,
    getWall,
    GroupPostEntry,
    GroupRoleEntry,
    GroupUserWithThumbnail,
    GroupWithShout, UserGroupV2
} from "../../../services/groups-typed";
import AuthenticationStore from "../../../stores/authentication";
import { wait } from "../../../lib/utils";
import { getRoles } from "../../../services/groups";
import {ThumbnailEntry} from "../../../services/thumbnailsT";
import { multiGetGroupIcons, multiGetUserHeadshots } from "../../../services/thumbnails";
import { getRobuxGroup } from "../../../services/economy";

const GroupsPageStore = createContainer(() => {
    const [group, setGroup] = useState<GroupFull|null>(null);
    const [posts, setPosts] = useState<GroupPosts|null>(null);
    const [members, setMembers] = useState<GroupMembers|null>(null);

    // TODO: should be in the other one, ill setup later
    const [userGroups, setUserGroups] = useState<UserGroupV2|null>(null);

    const [isLoading, setLoading] = useState(false);
    const [isLoadingNE, setLoadingNE] = useState(false); // ne = non-essential, like role perms and user stuff

    const auth = AuthenticationStore.useContainer();

    async function fetchData(group: GroupWithShout) {
        console.log("CHECKING, ", isLoading, auth.isPending);
        if (isLoading || auth.isPending) return;
        await setLoading(true);

        let groupIcon: ThumbnailEntry|null = null;
        try {
            // @ts-ignore
            groupIcon = (await multiGetGroupIcons({ groupIds: [group.id] }))[0];
        } catch (e) { console.error(e) }
        let groupRoles: GroupRoleEntry[] = [];
        try {
            groupRoles = (await getRoles({ groupId: group.id })).roles; // might be null
        } catch (e) { console.error(e) }
        try {
            let req = (await getWall({ groupId: group.id, sort: 'Desc', limit: 10, cursor: null}));
            if (req) {
                setPosts({
                    posts: req.data,
                    page: 1,
                    nextPage: req.nextPageCursor,
                    prevPage: req.previousPageCursor,
                });
            }
        } catch (e) { console.error(e) }
        try {
            let req = (await getMembers({ groupId: group.id, sortOrder: 'Desc', limit: 10, cursor: null}));
            if (req && req.data.length > 0) {
                console.dir(req);
                // @ts-ignore
                let memberThumbs = await multiGetUserHeadshots({userIds: req.data.map(v => v.user.userId)}) ?? [];
                setMembers({
                    members: req.data.map(v => {
                        let thumb = memberThumbs.find(d => d.targetId === v.user.userId);
                        return {
                            ...v,
                            imageUrl: thumb?.imageUrl ?? null,
                            state: thumb?.state ?? null,
                        }
                    }),
                    page: 1,
                    nextPage: req.nextPageCursor,
                    prevPage: req.previousPageCursor,
                });
            }
        } catch (e) { console.error(e) }
        let funds: {robux: number; tickets: number;} = { robux: 0, tickets: 0 };
        try {
            funds = await getRobuxGroup({groupId: group.id});
        } catch (e) { console.error(e) }

        setGroup({
            ...group,
            icon: groupIcon ?? null,
            roles: groupRoles ?? null,
            funds: funds ?? null,
            games: []
        });

        setTimeout(() => setLoading(false), 1000);
        await setLoadingNE(true);

        if (auth.isAuthenticated && auth.userId) {
            try {
                setUserGroups(await getUserGroupsV2({userId: auth.userId}));
            } catch (e) { console.error(e) }
        }

        await wait(1);
        setLoadingNE(false);
    }

    return {
        group, setGroup,
        isLoadingNE, setLoadingNE,
        posts, setPosts,
        members, setMembers,

        userGroups, setUserGroups,

        isLoading, setLoading,

        fetchData,
    }
});

export type GroupFull = GroupWithShout & {
    icon: ThumbnailEntry|null;
    roles: GroupRoleEntry[];
    games: null[];
    funds: {
        robux: number,
        tickets: number,
    };
};

export type GroupPosts = {
    posts: GroupPostEntry[];
    page: number;
    nextPage: string|null;
    prevPage: string|null;
}

export type GroupMembers = {
    members: GroupUserWithThumbnail[];
    page: number;
    nextPage: string|null;
    prevPage: string|null;
}

export default GroupsPageStore;