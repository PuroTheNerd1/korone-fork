import React, { useEffect, useState } from "react";
import { createUseStyles } from "react-jss"
import { getItemUrl } from "../../../services/catalog";
import GearDropdown from "../../gearDropdown";
import AssetListAdEntry from "./assetListAdEntry";
import AssetListCatalogEntry from "./assetListCatalogEntry";
import AssetListGameEntry from "./assetListGameEntry";
import thumbnailStore from "../../../stores/thumbnailStore";
import Link from "../../link";
import {
    getGameUrl,
    getLibraryItemUrl,
    isLibraryItem,
    launchStudio,
    shutdownPlaceServers
} from "../../../services/games";
import { getAssetThumbnail, getUniverseIcon } from "../../../services/thumbnails";
import ActionButton from "../../actionButton";
import useButtonStyles from "../../../styles/buttonStyles";
import AudioPlayButton from "../../catalogDetailsPage/components/audioPlayButton";
import AssetListGamePassEntry from "./assetListGamePassEntry";
import AssetListBadgeEntry from "./assetListBadgeEntry";
import getFlag from "../../../lib/getFlag";
import { FeedbackType } from "../../../models/feedback";
import Authentication from "../../../stores/authentication";
import FeedbackStore from "../../../stores/feedback";
import { wait } from "../../../lib/utils";

const useStyles = createUseStyles({
    image: {
        display: 'block',
        height: '70px',
        width: '70px',
        //objectFit: 'cover',
        margin: '0 6px 0 12px',
        cursor: 'pointer',
        userSelect: 'none',
    },
    row: {
        borderTop: '1px solid var(--white-color)',
        padding: '4px 0',
    },
    gearDropdownWrapper: {
        display: 'flex',
        justifyContent: 'flex-end',
        alignItems: 'center',
        float: 'right',
        marginLeft: 'auto',
        width: 'auto',
    },
    editWrapper: {
        marginRight: '10px'
    },
    editBtn: {
        padding: '0 7px',
        fontSize: 13,
        lineHeight: '24px',
        height: '25px',
    },
});

const AssetEntry = props => {
    const s = useStyles();
    const auth = Authentication.useContainer();
    const feedback = FeedbackStore.useContainer();
    const buttonStyles = useButtonStyles();
    const thumbs = thumbnailStore.useContainer();
    const isPlace = props.assetType === 9;
    const isAudio = props.assetType === 3;
    const isLibrary = isLibraryItem({ assetTypeId: props.assetType });
    const isAd = props.ad !== undefined && props.target !== undefined;
    const [thumbnail, setThumbnail] = useState('/img/placeholder/icon_one.png');
    const [isMobile, setMobile] = useState(false);
    
    const assetUrl = isPlace ? getGameUrl({
        placeId: props.assetId,
        name: props.name
    }) : isAd ? '#' : isLibrary ? getLibraryItemUrl({
        assetId: props.assetId,
        name: props.name
    }) : getItemUrl({ assetId: props.assetId, name: props.name })
    const genericAssetURL = isPlace ? `/games/${props.assetId}/--` : isLibrary ? `/library/${props.assetId}/--` : `/catalog/${props.assetId}/--`
    const url = isPlace ? `/universes/configure?id=${props.universeId}` : assetUrl;
    
    const imageAssetId = isAd ? props.ad.advertisementAssetId : props.assetId;
    // todo: figure out better way to do this
    const [runMenuOpen, setRunMenuOpen] = useState(false);
    
    const gearOptions = [
        isPlace && {
            url: '/universes/configure?id=' + props.universeId,
            name: 'Configure Game',
        },
        isPlace && {
            url: '/places/' + props.assetId + '/update',
            name: 'Configure Start Place',
        },
        // localization skipped
        isPlace && {
            name: 'separator',
        },
        isPlace && {
            name: 'Create Badge',
            url: `/develop?universeId=${props.universeId}&View=21`,
        },
        isPlace && {
            name: 'Create Pass',
            url: `/develop?universeId=${props.universeId}&View=34`,
        },
        isPlace && {
            name: 'Developer Stats',
            url: `/creations/games/${props.universeId}/stats`,
        },
        isPlace && {
            name: 'separator',
        },
        !isAd && !isPlace && {
            name: 'Configure',
            url: `/My/Item.aspx?id=${props.assetId}`,
        },
        !isAd && {
            name: 'Advertise',
            url: `/My/CreateUserAd.aspx?targetId=${props.assetId}&targetType=asset`,
        },
        isAd && {
            name: 'Run',
            onClick: e => {
                e.preventDefault();
                setRunMenuOpen(!runMenuOpen);
            },
        },
        isPlace && {
            name: 'separator',
        },
        isPlace && {
            name: 'Shut Down All Servers',
            onClick: e => {
                e.preventDefault();
                const confirmation = window.confirm("Do you want to shut down all servers?");
                if (confirmation) {
                    shutdownPlaceServers({ placeId: props.assetId }).then(() => {
                    }).catch(error => {
                        console.error('There was a problem shutting down all servers for a place:', error);
                        window.confirm(`There was a problem shutting down all servers: ${error}`)
                    })
                    /*fetch(`https://goober.top/rcc/killallservers?placeId=${props.assetId}`, {
                      method: "GET",
                    })
                      .then(response => {
                        if (!response.ok) {
                          throw new Error('Network response was not ok');
                        }
                      })
                      .catch(error => {
                        console.error('There was a problem with the fetch operation:', error);
                      });*/
                }
            },
        },
    ];
    
    useEffect(() => {
        if (thumbnail === '/img/placeholder/icon_one.png') {
            if (isPlace) {
                getUniverseIcon({ universeId: props.universeId }).then((result) => {
                    if (result?.data?.data[0]?.imageUrl)
                        setThumbnail(result.data.data[0].imageUrl);
                    if (result?.data?.data[0]?.state === 'Pending')
                        setThumbnail('/img/placeholder.png')
                })
            } else {
                getAssetThumbnail(imageAssetId).then((result) => {
                    if (result?.data?.data[0]?.imageUrl)
                        setThumbnail(result.data.data[0].imageUrl);
                    if (result?.data?.data[0]?.state === 'Pending')
                        setThumbnail('/img/placeholder.png')
                })
            }
        }
    }, [props?.ad?.advertisementAssetId, props?.assetId, props?.universeId])
    
    useEffect(() => {
        if (typeof navigator == 'undefined') return;
        const ua = navigator.userAgent.toLowerCase();
        if (
            /android/i.test(ua) ||
            !/windows/i.test(ua) &&
            !/linux/i.test(ua)
        ) {
            setMobile(true);
        }
    }, []);
    
    return <div className={`flex ${s.row}`}>
        <div className='col-2' style={{ padding: '0!important', width: 'auto!important' }}>
            <Link href={genericAssetURL}>
                <a href={genericAssetURL}>
                    <img className={s.image} src={thumbnail}/>
                    {isAudio && <AudioPlayButton small={true} audioId={imageAssetId}/>}
                </a>
            </Link>
        </div>
        <div className={
            //isPlace ? 'col-7 ps-0' :
            'col-7 ps-0'}>
            <p className='mb-0'>
                <Link href={url}>
                    <a>
                        {props.name}
                    </a>
                </Link>
            </p>
            { // oh my fucking god what did reformatting do to this???
                isAd ? <AssetListAdEntry ad={props.ad} target={props.target} runMenuOpen={runMenuOpen}
                                         setRunMenuOpen={setRunMenuOpen}/>
                     : props.assetType === 9 ?
                       <AssetListGameEntry url={assetUrl} startPlaceName={props.name}/>
                                             : props.assetType === 34 ? <AssetListGamePassEntry updated={props.updated}
                                                                                                sales={props.sales}
                                                                                                isForSale={props.isForSale}/>
                                                                      : props.assetType === 21 ? <AssetListBadgeEntry
                                                                                                   badge={props}/>
                                                                                               : <AssetListCatalogEntry
                                                                            created={props.created}/>
            }
        </div>
        <div className={`col-3 flex justify-content-end align-items-center flex-nowrap`}>
            {
                isPlace && !isMobile ? <div className={s.editWrapper}>
                    <ActionButton
                        noBtn
                        className={s.editBtn}
                        onClick={e => {
                            LaunchStudio(e, props.assetId, auth, feedback).then();
                        }}
                        disabled={!getFlag("studioEditButtonEnabled", false)}
                        label='Edit'
                        buttonStyle={buttonStyles.boxButton}
                    />
                </div> : null
            }
            <GearDropdown boxDropdownRightAmount={0} options={gearOptions.filter(v => !!v)}/>
        </div>
    </div>
}

const AssetList = props => {
    return <div className='row'>
        <div className='col-12'>
            {
                props.assets.map(v => {
                    return <AssetEntry key={v.assetId || v.ad.id} {...v} />
                })
            }
        </div>
    </div>
}

let deb = false;

/**
 * @param {any} e
 * @param {number} placeId
 * @param {{isAuthenticated: boolean}} auth
 * @param feedback
 * @constructor
 */
export const LaunchStudio = async (e, placeId, auth, feedback) => {
    if (deb) return;
    deb = true;
    console.log('blehhhhh :3');
    const ua = navigator.userAgent.toLowerCase();
    if (
        /android/i.test(ua) ||
        !/windows/i.test(ua) &&
        !/linux/i.test(ua)
    ) {
        return;
    }
    if (!auth.isAuthenticated) {
        window.location.href = "/auth/login";
    }
    if (!getFlag("studioEditButtonEnabled", false)) {
        return;
    }
    
    e && e.preventDefault && e.preventDefault();
    
    launchStudio({ placeId }).catch(err => {
        feedback.addFeedback(err.message, FeedbackType.ERROR);
    });
    await wait(1);
    deb = false;
}

export default AssetList;