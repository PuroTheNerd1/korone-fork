import {createContainer} from "unstated-next";
import {useEffect, useRef, useState} from "react";
import {
    getPermissionsForRoleset,
    getRolesetMembers,
    getUserGroupsV2,
    getWall,
    GroupPermissionsEntry,
    GroupPostEntry,
    GroupRoleEntry,
    GroupUserWithRoleIdThumbnail,
    GroupWithShout,
    UserGroupV2
} from "../../../services/groups-typed";
import AuthenticationStore from "../../../stores/authentication";
import {wait} from "../../../lib/utils";
import {getRoles} from "../../../services/groups";
import {ThumbnailEntry} from "../../../services/thumbnailsT";
import {multiGetAssetThumbnails, multiGetGroupIcons, multiGetUserHeadshots} from "../../../services/thumbnails";
import {getRobuxGroup} from "../../../services/economy";
import {getAssetDetailsClean, searchCatalog2} from "../../../services/catalog";
import {CatalogCategory, CatalogSortBy} from "../../CatalogPage/stores/CatalogPageStore";
import {userOwnsItems} from "../../../services/inventory";
import UserGroupsStore from "./UserGroupsStore";

const GroupsPageStore = createContainer(() => {
    const userGroupStore = UserGroupsStore.useContainer();
    const [group, setGroup] = useState<GroupFull|null>(null);
    const [posts, setPosts] = useState<GroupPosts>({posts: [], page: 0, nextPage: null, prevPage: null});
    const [members, setMembers] = useState<GroupMembers>({members: [], rank: 0, page: 0, nextPage: null, prevPage: null});
    const [memberCache, setMemberCache] = useState<GroupMembers[]>([]);

    const [userPerms, setUserPerms] = useState<GroupPermissionsEntry|null>(null);

    const [storeItems, setStoreItems] = useState<GroupStoreItems>({items: [], page: 0, total: 0, nextPage: null, prevPage: null});
    const [storeItemsCache, setStoreItemsCache] = useState<GroupStoreItems[]>([]);
    const sdeb = useRef(false);

    // TODO: should be in the other one, ill setup later

    const [isLoading, setLoading] = useState(false);
    const [isLoadingNE, setLoadingNE] = useState(false); // ne = non-essential, like role perms and user stuff

    const auth = AuthenticationStore.useContainer();

    useEffect(() => {
        if (!group) return;
        setUserPerms(null);
        let roleSetId = 1; // guest by default
        let userGroup = userGroupStore.userGroups.find(g => g.group.id === group?.id);
        if (userGroup) roleSetId = userGroup.role.id;

        (async () => {
            try {
                let req: GroupPermissionsEntry = await getPermissionsForRoleset({ groupId: group.id, rolesetId: roleSetId });
                if (req) setUserPerms(req);
            } catch (e) { console.error(e) }
        })()
    }, [userGroupStore.userGroups, group]);

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
            groupRoles = await getRoles({ groupId: group.id }); // might be null
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
            if (!groupRoles || groupRoles.length <= 0 || groupRoles.filter(v=>v.id>1).length <= 0) throw new Error("no roles to process group members");
            let rankId = groupRoles.filter(v => v.id > 1)[0]?.id;
            if (rankId === undefined) throw new Error("no default rank found for group members")
            let req = (await getRolesetMembers({ groupId: group.id, roleSetId: rankId, sortOrder: 'Desc', limit: 9, cursor: null}));
            if (req && req.data) {
                // @ts-ignore
                let memberThumbs = await multiGetUserHeadshots({userIds: req.data.map(v => v.userId)}) ?? [];
                let members = {
                    members: req.data.map(v => {
                        let thumb = memberThumbs.find(d => d.targetId === v.userId);
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
                };
                setMembers(members);
                if (clearData) {
                    setMemberCache([members]);
                } else {
                    setMemberCache([...memberCache, members]);
                }
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

        await userGroupStore.fetchData();

        setTimeout(() => setLoading(false), 1000);
        await setLoadingNE(true);

        await wait(1);
        setLoadingNE(false);
    }

    async function fetchMembers(rank: number, page: number, cursor: string) {
        try {
            if (!group.roles || group.roles.length <= 0 || group.roles.filter(v=>v.id>1).length <= 0) throw new Error("no roles to process group members");
            let memberCached = memberCache.find(mc => mc.rank === rank && mc.page === page);
            if (memberCached) {
                setMembers(memberCached);
                return;
            }

            let req = (await getRolesetMembers({ groupId: group.id, roleSetId: rank, sortOrder: 'Desc', limit: 9, cursor: cursor}));
            if (req && req.data) {
                // @ts-ignore
                let memberThumbs = await multiGetUserHeadshots({userIds: req.data.map(v => v.userId)}) ?? [];
                let members = {
                    members: req.data.map((v: { userId: number; }) => {
                        let thumb = memberThumbs.find(d => d.targetId === v.userId);
                        return {
                            ...v,
                            imageUrl: thumb?.imageUrl ?? null,
                            state: thumb?.state ?? null,
                        }
                    }),
                    rank: rank,
                    page: page,
                    nextPage: req.nextPageCursor,
                    prevPage: req.previousPageCursor,
                };
                setMembers(members);
                setMemberCache([...memberCache, members]);
            } else {
                console.error("failed to fetch members for rank " + rank);
            }
        } catch (e) { console.error(e) }
    }

    async function fetchStoreItems(page: number, cursor: string) {
        try {
            if (!group?.id || sdeb.current) return;
            let storeItemsCached = storeItemsCache.find(gsi => gsi.page === page);
            if (storeItemsCached) {
                setStoreItems(storeItemsCached);
                return;
            }

            try {
                let success = await loadItems(page, cursor);
                console.log("loading success: " + success);
            } catch (e) {
                console.error("failed to load store items for group " + group.id);
                throw e;
            }
        } catch (e) { console.error(e) }
    }

    async function loadItems(page: number, cursor: string) {
        sdeb.current = true;
        // @ts-ignore
        const searchResultsFlat = await searchCatalog2({
            category: CatalogCategory.All,
            sort: CatalogSortBy.RecentlyUpdated,
            creatorType: 2,
            creatorId: group.id,
            limit: 24,
            cursor: cursor,
        });
        let newResult: GroupStoreItems = {
            items: [],
            page: page,
            total: searchResultsFlat._total,
            nextPage: searchResultsFlat.nextPageCursor,
            prevPage: searchResultsFlat.previousPageCursor,
        }
        if (searchResultsFlat.data.length === 0) {
            setStoreItems(newResult);
            setStoreItemsCache([...storeItemsCache, newResult]);
            await wait(0.75);
            sdeb.current = false;
            return false;
        }

        const searchResultsRaw = await getAssetDetailsClean(searchResultsFlat.data);
        if (!searchResultsRaw) { console.dir("Failed to load asset details from search results: " + searchResultsFlat); setStoreItems(newResult); await wait(0.75); sdeb.current = false; return false; }

        const thumbnails = await multiGetAssetThumbnails({ assetIds: searchResultsRaw.map(d => d.id) });
        // @ts-ignore
        const ownsAssets: { id: number; owned: boolean; }[] = auth?.isAuthenticated && auth?.userId ? await userOwnsItems({ userId: auth?.userId, assetIds: searchResultsRaw.map(d => d.id) }) : [];
        // @ts-ignore
        newResult.items = searchResultsRaw.map(d => {
            let thumb = thumbnails.find(t => t.targetId === d.id);
            let ownsAsset = ownsAssets.find(t => t.id === d.id);
            return {
                ...d,
                state: thumb?.state ?? null,
                imageUrl: thumb?.imageUrl ?? null,
                owned: ownsAsset?.owned ?? false,
            }
        });

        setStoreItems(newResult);
        setStoreItemsCache([...storeItemsCache, newResult]);
        await wait(0.75);
        sdeb.current = false;
        return true;
    }

    return {
        group, setGroup,
        isLoadingNE, setLoadingNE,
        posts, setPosts,
        members, setMembers,

        storeItems, setStoreItems,

        userPerms, setUserPerms,

        isLoading, setLoading,

        sdeb,

        fetchData,
        fetchMembers,
        fetchStoreItems,
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

export type GroupStoreItems = {
    items: CatalogAssetDetails[];
    page: number;
    total: number;
    nextPage: string|null;
    prevPage: string|null;
}

export type CatalogAssetDetails = {
    id: number;
    assetType: number;
    name: string;
    description: string;
    genres: string[];
    creatorType: "User" | "Group";
    creatorTargetId: number;
    creatorName: string;
    offsaleDeadline: string | null;
    itemRestrictions: ("Limited" | "LimitedUnique")[];
    saleCount: number;
    itemType: "Asset" | string;
    favoriteCount: number;
    isForSale: boolean;
    commentsEnabled: boolean;
    price: number | null;
    priceTickets: number | null;
    lowestPrice: number | null;
    priceStatus: string | null;
    lowestSellerData: {
        userId: number;
        username: string;
        userAssetId: number;
        price: number;
        assetId: number;
    } | null;
    unitsAvailableForConsumption: number | null;
    serialCount: number;
    is18Plus: boolean;
    moderationStatus: "ReviewApproved" | string;
    createdAt: string;
    updatedAt: string;
    state?: string;
    imageUrl?: string;
    owned?: boolean;
}

export default GroupsPageStore;