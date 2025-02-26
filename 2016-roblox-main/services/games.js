import axios from "axios";
import getFlag from "../lib/getFlag";
import request, {getBaseUrl, getFullUrl} from "../lib/request"
import {itemNameToEncodedName} from "./catalog";

const gamePage2015Enabled = getFlag('2015GameDetailsPageEnabled', false);
const csrEnabled = getFlag('clientSideRenderingEnabled', false);

export const isLibraryItem = ({assetTypeId}) => {
    if (assetTypeId === 62 ||
        assetTypeId === 40 ||
        assetTypeId === 38 ||
        assetTypeId === 24 ||
        assetTypeId === 13 ||
        assetTypeId === 10 ||
        assetTypeId === 5 ||
        assetTypeId === 4 ||
        assetTypeId === 3 ||
        assetTypeId === 1) {
        return true;
    } else {
        return false;
    }
}

export const getGameUrl = ({placeId, name}) => {
    return `/games/${placeId}/${itemNameToEncodedName(name)}`;
}

export const getLibraryItemUrl = ({assetId, name}) => {
    return `/library/${assetId}/${itemNameToEncodedName(name)}`;
}

export const getUserGames = ({userId, cursor}) => {
    return request('GET', getFullUrl('games', `/v2/users/${userId}/games?cursor=${encodeURIComponent(cursor || '')}`)).then(d => d.data);
}

export const getGroupGames = ({groupId, cursor}) => {
    return request('GET', getFullUrl('games', `/v2/groups/${groupId}/games?cursor=${encodeURIComponent(cursor || '')}`)).then(d => d.data);
}

export const getGameSorts = ({gameSortsContext}) => {
    return request('GET', getFullUrl('games', `/v1/games/sorts?gameSortsContext=${encodeURIComponent(gameSortsContext || '')}`)).then(d => d.data)
}

export const getRecommendedGames = ({placeId, limit}) => {
    return request('GET', getFullUrl('games', `/v1/games/recommendations/game/${placeId}?maxRows=${limit}`)).then(d => d.data)
}

export const getGameList = ({sortToken, limit, genre = 0, keyword}) => {
    return request('GET', getFullUrl('games', `/v1/games/list?sortToken=${encodeURIComponent(sortToken)}&maxRows=${limit}&genre=${genre}&keyword=${keyword}`)).then(d => d.data)
}

export const getGameMedia = ({universeId}) => {
    return request('GET', getFullUrl('games', `/v2/games/${universeId}/media`)).then(d => d.data.data);
}

export const launchGame = async ({placeId}) => {
    const result = await request('GET', getBaseUrl() + '/game/get-join-script?placeId=' + encodeURIComponent(placeId));
    const toClick = result.data.joinUrl;
    const aTag = document.createElement('a');
    aTag.setAttribute('href', result.data.prefix + '' + result.data.joinScriptUrl);
    document.body.appendChild(aTag);
    aTag.click();
    // delay before deletion is required on some browsers, not sure why
    setTimeout(() => {
        aTag.remove();
    }, 1000);
}

export const launchGameFromJobId = async ({placeId, jobId}) => {
    
    if (navigator.userAgent.includes("ROBLOX Android App") || navigator.userAgent.includes("ROBLOX iOS App")) {
        window.location.href = 'games/start?placeid=' + placeId;
        return;
    }
    
    const result = await request('GET', getBaseUrl() + '/game/get-join-script-fromjobid?placeId=' + encodeURIComponent(placeId) + "&jobId=" + encodeURIComponent(jobId));
    const toClick = result.data.joinUrl;
    const aTag = document.createElement('a');
    aTag.setAttribute('href', result.data.prefix + '' + result.data.joinScriptUrl);
    document.body.appendChild(aTag);
    aTag.click();
    // delay before deletion is required on some browsers, not sure why
    setTimeout(() => {
        aTag.remove();
    }, 1000);
}

export const multiGetPlaceDetails = ({placeIds}) => {
    return request('GET', getFullUrl('games', `/v1/games/multiget-place-details?placeIds=${encodeURIComponent(placeIds.join(','))}`)).then(d => d.data);
}

export const multiGetUniverseDetails = ({universeIds}) => {
    return request('GET', getFullUrl('games', `/v1/games?universeIds=${encodeURIComponent(universeIds.join(','))}`)).then(d => d.data.data);
}

export const getServers = ({placeId, offset}) => {
    return request('GET', getBaseUrl() + `/games/getgameinstancesjson?placeId=${placeId}&startIndex=${offset}`).then(d => d.data);
}

export const multiGetGameVotes = ({universeIds}) => {
    return request('GET', getFullUrl('games', '/v1/games/votes?universeIds=' + encodeURIComponent(universeIds.join(',')))).then(d => d.data.data);
}

export const voteOnGame = ({universeId, isUpvote}) => {
    return request('PATCH', getFullUrl('games', '/v1/games/' + universeId + '/user-votes'), {
        vote: isUpvote,
    }).then(d => d.data.data);
}

export const shutdownPlaceServers = ({placeId}) => {
    return request('GET', getBaseUrl() + `/rcc/killallservers?placeId=${placeId}`).then(d => d.data);
}

export const shutdownSpecificServer = ({placeId, jobId}) => {
    return request('GET', getBaseUrl() + `/rcc/killserver?placeId=${placeId}&jobId=${jobId}`).then(d => d.data);
}

export const getUniverseGamePasses = ({ universeId }) => {
    return request('GET', getFullUrl('games', `/v1/games/${universeId}/game-passes`)).then(d => d.data.data);
}

export const getGameTemplates = () => {
    //return request('GET', getBaseUrl('/v1/gametemplates')).then(d => d.data.data);
    return new Promise((res, rej) => {
        res({
            data: {
                data: [
                    {
                        gameTemplateType: "Generic",
                        hasTutorials: false,
                        universe: {
                            id: 852,
                            name: "Starting Place",
                            description: "",
                            isArchived: false,
                            rootPlaceId: 36568,
                            tempThumbnailId: 5,
                            isActive: true,
                            privacyType: "Public",
                            creatorType: "User",
                            creatorTargetId: 1,
                            creatorName: "ROBLOX",
                            created: "2025-02-11T09:21:56.256878Z",
                            updated: "2025-02-11T10:02:01.168426Z"
                        }
                    },
                    {
                        gameTemplateType: "Generic",
                        hasTutorials: false,
                        universe: {
                            id: 852,
                            name: "Western",
                            description: "",
                            isArchived: false,
                            rootPlaceId: 36569,
                            tempThumbnailId: 6,
                            isActive: true,
                            privacyType: "Public",
                            creatorType: "User",
                            creatorTargetId: 1,
                            creatorName: "ROBLOX",
                            created: "2025-02-11T09:21:56.256878Z",
                            updated: "2025-02-11T10:02:01.168426Z"
                        }
                    },
                    {
                        gameTemplateType: "Generic",
                        hasTutorials: false,
                        universe: {
                            id: 852,
                            name: "Line Runner",
                            description: "",
                            isArchived: false,
                            rootPlaceId: 36570,
                            tempThumbnailId: 6,
                            isActive: true,
                            privacyType: "Public",
                            creatorType: "User",
                            creatorTargetId: 1,
                            creatorName: "ROBLOX",
                            created: "2025-02-11T09:21:56.256878Z",
                            updated: "2025-02-11T10:02:01.168426Z"
                        }
                    },
                    {
                        gameTemplateType: "Generic",
                        hasTutorials: false,
                        universe: {
                            id: 852,
                            name: "Village",
                            description: "",
                            isArchived: false,
                            rootPlaceId: 36571,
                            tempThumbnailId: 6,
                            isActive: true,
                            privacyType: "Public",
                            creatorType: "User",
                            creatorTargetId: 1,
                            creatorName: "ROBLOX",
                            created: "2025-02-11T09:21:56.256878Z",
                            updated: "2025-02-11T10:02:01.168426Z"
                        }
                    },
                    {
                        gameTemplateType: "Generic",
                        hasTutorials: false,
                        universe: {
                            id: 852,
                            name: "Racing",
                            description: "",
                            isArchived: false,
                            rootPlaceId: 36572,
                            tempThumbnailId: 6,
                            isActive: true,
                            privacyType: "Public",
                            creatorType: "User",
                            creatorTargetId: 1,
                            creatorName: "ROBLOX",
                            created: "2025-02-11T09:21:56.256878Z",
                            updated: "2025-02-11T10:02:01.168426Z"
                        }
                    },
                    {
                        gameTemplateType: "Generic",
                        hasTutorials: false,
                        universe: {
                            id: 852,
                            name: "City",
                            description: "",
                            isArchived: false,
                            rootPlaceId: 36573,
                            tempThumbnailId: 6,
                            isActive: true,
                            privacyType: "Public",
                            creatorType: "User",
                            creatorTargetId: 1,
                            creatorName: "ROBLOX",
                            created: "2025-02-11T09:21:56.256878Z",
                            updated: "2025-02-11T10:02:01.168426Z"
                        }
                    },
                ]
            }
        });
    })
}