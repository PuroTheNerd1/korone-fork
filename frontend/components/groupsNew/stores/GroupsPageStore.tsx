import {createContainer} from "unstated-next";
import {useEffect, useState} from "react";
import {
    getPermissionsForRoleset,
    getRolesetMembers, getUserGroupsV2,
    getWall, GroupPermissionsEntry,
    GroupPostEntry,
    GroupRoleEntry, GroupUserWithRoleIdThumbnail,
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
    const [posts, setPosts] = useState<GroupPosts>({posts: [], page: 0, nextPage: null, prevPage: null});
    const [members, setMembers] = useState<GroupMembers>({members: [], rank: 0, page: 0, nextPage: null, prevPage: null});
    const [memberCache, setMemberCache] = useState<GroupMembers[]>([]);

    // TODO: should be in the other one, ill setup later
    const [userGroups, setUserGroups] = useState<UserGroupV2[]>([]);
    const [userPerms, setUserPerms] = useState<GroupPermissionsEntry|null>(null);

    const [isLoading, setLoading] = useState(false);
    const [isLoadingNE, setLoadingNE] = useState(false); // ne = non-essential, like role perms and user stuff

    const auth = AuthenticationStore.useContainer();

    useEffect(() => {
        if (!group) return;
        setUserPerms(null);
        let roleSetId = 1; // guest by default
        let userGroup = userGroups.find(g => g.group.id === group?.id);
        if (userGroup) roleSetId = userGroup.role.id;

        (async () => {
            try {
                let req: GroupPermissionsEntry = await getPermissionsForRoleset({ groupId: group.id, rolesetId: roleSetId });
                if (req) setUserPerms(req);
            } catch (e) { console.error(e) }
        })()
    }, [userGroups, group]);

    async function fetchData(group: GroupWithShout, clearData?: boolean) {
        console.log("CHECKING, ", isLoading, auth.isPending);
        if (isLoading || auth.isPending) return;
        await setLoading(true);

        if (clearData) {
            // reset everything
            await setGroup(null);
            await setPosts({posts: [], page: 0, nextPage: null, prevPage: null});
            await setMembers({members: [], rank: 0, page: 0, nextPage: null, prevPage: null});
        }

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
            if (!groupRoles || groupRoles.length <= 0 || groupRoles.filter(v=>v.rank!==0).length <= 0) throw new Error("no roles to process group members");
            let rankId = groupRoles
                .filter(v => v.rank !== 0)
                .sort((a, b) => b.rank - a.rank)
                [0]?.rank;
            if (rankId === undefined) throw new Error("no default rank found for group members")
            let req = (await getRolesetMembers({ groupId: group.id, roleSetId: rankId, sortOrder: 'Desc', limit: 9, cursor: null}));
            if (req && req.data.length > 0) {
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
                    rank: rankId,
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
                setUserGroups(await getUserGroupsV2({userId: auth.userId}) || []);
            } catch (e) { console.error(e) }
        }

        await wait(1);
        setLoadingNE(false);
    }

    async function fetchMembers(rank: number, page: number, cursor: string) {
        try {
            if (!group.roles || group.roles.length <= 0 || group.roles.filter(v=>v.rank!==0).length <= 0) throw new Error("no roles to process group members");
            let memberCached = memberCache.find(mc => mc.rank === rank && mc.page === page);
            if (memberCached) {
                setMembers(memberCached);
                return;
            }

            let rankId = group.roles
                .filter(v => v.rank !== 0)
                .sort((a, b) => b.rank - a.rank)
                [0]?.rank;
            if (rankId === undefined) throw new Error("no default rank found for group members")
            let req = (await getRolesetMembers({ groupId: group.id, roleSetId: rankId, sortOrder: 'Desc', limit: 9, cursor: cursor}));
            if (req && req.data.length > 0) {
                // @ts-ignore
                let memberThumbs = await multiGetUserHeadshots({userIds: req.data.map(v => v.user.userId)}) ?? [];
                let members = {
                    members: req.data.map(v => {
                        let thumb = memberThumbs.find(d => d.targetId === v.user.userId);
                        return {
                            ...v,
                            imageUrl: thumb?.imageUrl ?? null,
                            state: thumb?.state ?? null,
                        }
                    }),
                    rank: rankId,
                    page: page,
                    nextPage: req.nextPageCursor,
                    prevPage: req.previousPageCursor,
                };
                setMembers(members);
                setMemberCache([...memberCache, members]);
            }
        } catch (e) { console.error(e) }
    }

    return {
        group, setGroup,
        isLoadingNE, setLoadingNE,
        posts, setPosts,
        members, setMembers,

        userGroups, setUserGroups,

        userPerms, setUserPerms,

        isLoading, setLoading,

        fetchData,
        fetchMembers,
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
    members: GroupUserWithRoleIdThumbnail[];
    rank: number;
    page: number;
    nextPage: string|null;
    prevPage: string|null;
}

export default GroupsPageStore;