import { createContainer } from "unstated-next";
import { useEffect, useRef, useState } from "react";
import AuthenticationStore from "../../../stores/authentication";
import { List } from "../../../models/CS";
import { getAssetDetailsClean, getNavigationMenuItems, searchCatalog2 } from "../../../services/catalog";
import { multiGetAssetThumbnails } from "../../../services/thumbnails";
import { userOwnsItems } from "../../../services/inventory";
import { IsNullOrEmpty, wait } from "../../../lib/utils";

const CatalogPageStore = createContainer(() => {
    const auth = AuthenticationStore.useContainer();
    
    /** @type {[CatalogAssetDetails[], import('react').Dispatch<CatalogAssetDetails[]>]} */
    const [results, setResults] = useState([]);
    const [total, setTotal] = useState(0);
    /** @type {[CatalogResultsMetadata, import('react').Dispatch<CatalogResultsMetadata>]} */
    const [resultMetadata, setResultMetadata] = useState({
        limit: 30,
        page: 1,
        cursor: null,
        nextCursor: null,
        prevCursor: null,
    });
    
    const [categoryNav, setCategoryNav] = useState(/** @type {CatalogCategory[]} */([]));
    const [genreNav, setGenreNav] = useState(/** @type {GenreClass[]} */([]));
    
    const [category, setCategory] = useState(CatalogCategory.Featured);
    const [subCategory, setSubCategory] = useState(CatalogSubCategory.All);
    const [sortBy, setSortBy] = useState(CatalogSortBy.Relevance);
    const [genres, setGenres] = useState(new List());
    // 0 == any, 1 == price range, 2 == free
    const [priceOption, setPriceOption] = useState(0);
    const [priceRange, setPriceRange] = useState([0, 0]);
    const [selectedCurrency, setSelectedCurrency] = useState(3);
    const [includeOffSale, setIncludeOffSale] = useState(false);
    const [creatorOption, setCreatorOption] = useState(1);
    const [creator, setCreator] = useState(""); // search user first, then group (thats how this works). if its null that means any

    const [searchInput, setSearchInput] = useState("");
    
    const refreshDebounce = useRef(false);
    const [isLoading, setLoading] = useState(false);
    const [navVisible, setNavVisible] = useState(false);

    /**
     * @typedef CatalogCurrentOptions
     * @property category
     * @property subCategory
     * @property sortBy
     * @property genres
     * @property priceOption
     * @property priceRange
     * @property selectedCurrency
     * @property includeOffSale
     * @property creator
     */

    function getCurrentPage(total, nextPageCursor, limit) {
        total = Number(total);
        limit = Number(limit);
        if (!Number.isSafeInteger(total) || !Number.isSafeInteger(limit) || limit <= 0) {
            return 1;
        }

        if (nextPageCursor == null) {
            return Math.ceil(total / limit);
        }
        nextPageCursor = Number(nextPageCursor);
        if (!Number.isSafeInteger(nextPageCursor) || nextPageCursor < 0) {
            return 1;
        }

        return Math.max(1, Math.floor(nextPageCursor / limit));
    }

    async function RefreshCatalogItems(e, reloadPage = false, arr = {}, creatorOptionReq = creatorOption, cursor = "") {
        if (refreshDebounce.current) return false;
        refreshDebounce.current = true;
        setLoading(true);
        if (e?.preventDefault) e?.preventDefault();
        
        /**
         * @type {CatalogCurrentOptions}
         */
        let options = {
            category: category,
            subCategory: subCategory,
            genres: genres,
            sortBy: sortBy,
            priceOption: priceOption,
            priceRange: priceRange,
            selectedCurrency: selectedCurrency,
            includeOffSale: includeOffSale,
            creator: creatorOptionReq === 1 ? null : creatorOptionReq === 2 ? "ROBLOX" : creator,
            ...arr,
        };
        let gen = options.genres.ToArray();
        const searchResultsFlat = await searchCatalog2({
            category: options.category,
            subCategory: options.subCategory,
            genres: gen.length === 0 || gen.length === 1 && gen[0] === 0 ? null : gen,
            query: !IsNullOrEmpty(searchInput) ? searchInput : null,
            includeNotForSale: options.includeOffSale,
            limit: resultMetadata.limit,
            cursor: (!IsNullOrEmpty(resultMetadata.cursor) || !IsNullOrEmpty(cursor)) && !reloadPage ? cursor : null,
            sort: options.sortBy,
            creatorName: !IsNullOrEmpty(options.creator) ? options.creator : null,
            priceOption: options.priceOption,
            priceRange: options.priceRange,
            currency: options.selectedCurrency,
        });
        setResults([]);
        if (reloadPage) setTotal(0);
        setResultMetadata({
            nextCursor: searchResultsFlat.nextPageCursor,
            prevCursor: searchResultsFlat.previousPageCursor,
            limit: 30,
            cursor: reloadPage ? null : cursor,
            page: reloadPage ? 1 : getCurrentPage(searchResultsFlat._total, searchResultsFlat.nextPageCursor, resultMetadata.limit),
        });
        if (searchResultsFlat.data.length === 0) {
            setResults(searchResultsFlat.data);
            setTotal(searchResultsFlat._total || 0);
            await wait(0.75);
            refreshDebounce.current = false;
            setLoading(false);
            return true;
        }
        const searchResultsRaw = await getAssetDetailsClean(searchResultsFlat.data);
        if (!searchResultsRaw) { refreshDebounce.current = false; setLoading(false); console.log("nooo"); return false; }
        if (e?.setSelSuccess)
            e.setSelSuccess(true)
        const thumbnails = await multiGetAssetThumbnails({ assetIds: searchResultsRaw.map(d => d.id) });
        /** @type {{id: number; owned: boolean;}[]} */
        const ownsAssets = auth?.userId ? await userOwnsItems({ userId: auth?.userId, assetIds: searchResultsRaw.map(d => d.id) }) : [];
        /** @type CatalogAssetDetails[] */
        let searchResults = searchResultsRaw.map(d => {
            let thumb = thumbnails.find(t => t.targetId === d.id);
            let ownsAsset = ownsAssets.find(t => t.id === d.id);
            return {
                ...d,
                state: thumb?.state ?? null,
                imageUrl: thumb?.imageUrl ?? null,
                owned: ownsAsset?.owned ?? false,
            }
        });
        setResults(searchResultsFlat.data.map(v => {
            return searchResults.find(d => v.id === d.id);
        }));
        setTotal(searchResultsFlat._total || 0);
        
        await wait(0.75);
        refreshDebounce.current = false;
        setLoading(false);
        return true
    }
    
    function AddGenre(genre) {
        let clone = genres.Clone();
        clone.Add(genre);
        if (clone === genres) return clone;
        setGenres(clone);
        return clone;
    }
    function RemoveGenre(genre) {
        let clone = genres.Clone();
        clone.Remove(genre);
        if (clone === genres) return clone;
        setGenres(clone);
        return clone;
    }
    
    useEffect(async () => {
        let catalogNav = await getNavigationMenuItems();
        setCategoryNav(catalogNav.categories);
        setGenreNav(catalogNav.genres);
    }, []);
    
    return {
        RefreshCatalogItems,
        refreshDebounce,
        AddGenre,
        RemoveGenre,
        
        /** @type CatalogAssetDetails[] */
        results,
        setResults,

        /** @type number */
        total,
        
        /** @type CatalogResultsMetadata */
        resultMetadata,
        setResultMetadata,
        
        categoryNav,
        genreNav,

        isLoading,

        category,
        setCategory,
        subCategory,
        setSubCategory,
        sortBy,
        setSortBy,
        genres,
        setGenres,
        includeOffSale,
        setIncludeOffSale,
        creator,
        setCreator,
        creatorOption,
        setCreatorOption,

        priceOption,
        setPriceOption,
        priceRange,
        setPriceRange,
        selectedCurrency,
        setSelectedCurrency,
        
        searchInput,
        setSearchInput,

        navVisible,
        setNavVisible,
    }
});

export default CatalogPageStore;

export const CatalogCategory = Object.freeze({
    Featured: 0,
    All: 1,
    Collectibles: 2,
    Accessories: 11,
    Clothing: 3,
    BodyParts: 4,
    Gears: 5,
    AvatarAnimations: 12,
    Emotes: 13,
    Special: 69,
});
export const CatalogSubCategory = Object.freeze({
    All: 0,
    Faces: 10,
    Packages: 37,
    Shirts: 12,
    TeeShirts: 13,
    Pants: 14,
    // GEAR
    Building: 8,
    Explosive: 3,
    Melee: 1,
    Musical: 6,
    Navigation: 5,
    Powerup: 4,
    Ranged: 2,
    Social: 7,
    Transport: 9,
    //
    Accessories: 19,
    Animations: 39,
    Gear: 5,
    Emotes: 39,
    Heads: 15,
    Hats: 9,
    Hair: 20,
    Neck: 22,
    Shoulder: 23,
    Front: 24,
    Back: 25,
    Waist: 26,
    
    Owned: 101,
    Wearing: 102,
});
export const CatalogSortBy = Object.freeze({
    Relevance: 0,
    RecentlyUpdated: 3,
    PriceAsc: 4,
    PriceDesc: 5,
    RAPAsc: 6,
    RAPDesc: 7,
});
export const StringToCategory = (str) => {
    // these are from catalog.roblox.com/v1/search/navigation-menu-items
    switch (str.toLowerCase().trim()) {
        case 'collectible':
        case 'collectibles':
            return 2;
        case 'featured':
            return 0;
        case 'accessories':
            return 11;
        case 'clothing':
            return 3;
        case 'gears':
        case 'gear':
            return 5;
        case 'bodyparts':
            return 4;
    }                            
    throw new Error('Invalid category "' + str + '"');
}
export const StringToSubCategory = (str) => {
    // these are from catalog.roblox.com/v1/search/navigation-menu-items
    switch (str.toLowerCase().trim()) {
        case 'items':
        case 'hats':
            return 0; // todo: what do we put here?
        case 'all':
            return 0;
        case 'face':
        case 'faces':
            return 10;
        case 'packages':
            return 37; // todo: is this correct?
        case 'shirts':
            return 12;
        case 'tshirts':
            return 13;
        case 'pants':
            return 14;
        // gear categories
        case 'gear':
            return 0;
        case 'building':
            return 8;
        case 'explosive':
            return 3;
        case 'melee':
            return 1;
        case 'musical':
            return 6;
        case 'navigation':
            return 5;
        case 'powerup':
            return 4;
        case 'ranged':
            return 2;
        case 'social':
            return 7;
        case 'transport':
            return 9;
    }
    throw new Error('Invalid subcategory "' + str + '"');
}
export const SortByToString = (num) => {
    switch (num) {
        case 0:
            return "Relevance";
        case 3:
            return "Recently Updated";
        case 4:
            return "Price (Low to High)"
        case 5:
            return "Price (High to Low)"
        case 6:
            return "RAP (Low to High)"
        case 7:
            return "RAP (High to Low)"
        default:
            throw new Error(`Unknown sort by: ${num}`);
    }
}
export const FormatCamelCase = (str) => {
    if (IsNullOrEmpty(str)) {
        return null;
    }
    return str.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/\b\w/g, char => char.toUpperCase());
}
export const EnumToString = (num, freeze) => {
    return Object.keys(freeze || {}).find(k => freeze[k] === num);
}
