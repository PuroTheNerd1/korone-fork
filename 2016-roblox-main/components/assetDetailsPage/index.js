import { createUseStyles } from "react-jss";
import AssetDetailsStore from "./stores/AssetDetailsStore";
import Feedback from "../../stores/feedback";
import React, { useEffect, useState } from "react";
import UserAdvertisement from "../userAdvertisement";
import { AssetType, UserAdvertisementType } from "../../models/enums";
import ItemImage from "../itemImage";
import Link from "../link";
import CreatorLink from "../creatorLink";
import useButtonStyles from "../../styles/buttonStyles";
import ActionButton from "../actionButton";
import { genresList } from "../develop/components/library/genreFilter";
import AssetDetailsModalStore from "./stores/AssetDetailsModalStore";
import AssetRecommendations from "./components/AssetRecommendations";
import Theme2016 from "../theme2016";
import FavouriteButton from "./components/FavouriteButton";
import Countdown from "./components/Countdown";
import AssetDropdown from "./components/Dropdown";
import PriceChart from "./components/PriceChart";
import Resellers from "./components/Resellers";
import BuyModal from "./modals/BuyModal";
import { avPageStyleType, getAvPageStyle } from "../../services/theme";
import { AssetTypeCategory } from "../AvatarEditorPage/stores/avatarInfoStore";
import SellItemModal from "./modals/SellItemModal";
import ConfirmSellModal from "./modals/ConfirmSellModal";
import Authentication from "../../stores/authentication";
import Owners from "./components/Owners";
import RelatedGame from "./components/RelatedGame";
import AudioPlayButton from "../catalogDetailsPage/components/audioPlayButton";

const useStyles = createUseStyles({
    pageWrapper: {
        display: "flex",
        gap: 15,
        justifyContent: "flex-end",
    },
    container: {
        width: "calc(100% - 185px)",
        maxWidth: 950,
    },
    itemContainer: {
        display: "flex",
        flexDirection: "row",
        gap: 12,
        // boxShadow: "none",
        "& *": {
            textAlign: "start",
            fontWeight: 500,
            fontSize: 16,
        }
    },
    
    itemThumbContainer: {
        width: 420,
    },
    itemDetailsContainer: {
        width: "calc(100% - 432px)",
    },
    itemHeaderContainer: {
        paddingBottom: 12,
        borderBottom: "1px solid #e3e3e3",
        position: "relative",
        "& h2": {
            fontSize: 32,
            fontWeight: 800,
            lineHeight: "1em",
            padding: "5px 0",
            margin: 0,
            maxHeight: "2.225em",
            overflow: "hidden",
            whiteSpace: "nowrap",
            wordBreak: "break-word",
            textOverflow: "ellipsis",
        },
    },
    itemDescription: {
        fontWeight: 300,
        whiteSpace: "pre-line",
        width: "calc(100% - 120px)",
        fontFamily: 'HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif'
    },
    smallImg: {
        width: 420,
        height: 420,
        display: "flex",
        alignItems: "center",
        "& img": {
            width: 150,
            height: 150,
        }
    },
    
    img: { padding: 0 },
    
    itemDetails: {},
    
    priceWrapper: {
        display: "flex",
        justifyContent: "space-between",
        width: "calc(100% - 120px)",
    },
    editWrapper: {
        display: "flex",
        justifyContent: "space-between",
        width: "180px",
    },
    buyBtn: {
        fontWeight: 500,
        width: 180,
        padding: 15,
        lineHeight: "100%",
        borderRadius: 5,
        fontSize: 21,
        textAlign: "center",
    },
    editBtn: {
        fontSize: 18,
        padding: 9,
        borderRadius: 3,
    },
    buyBtnContainer: {
        position: "relative",
    },
    priceIcon: { marginRight: 3, },
    priceLabel: {
        lineHeight: "1.16em",
        fontSize: 20,
        fontWeight: 700,
        marginTop: 2,
    },
    
    attrContainer: {
        display: "flex",
        width: '100%',
        marginTop: 12,
    },
    attrLabel: {
        fontSize: 16,
        fontWeight: 500,
        lineHeight: "1.4em",
        color: "var(--text-color-secondary)",
        width: 120,
        paddingRight: 9,
        display: "inline-block",
    },
    
    restrictionsContainer: {
        position: 'absolute',
        bottom: -3,
        left: -3,
        overflow: 'hidden',
    },
    
    itemInteractionContainer: {},
    
    itemStatusContainer: {
        position: 'absolute',
        top: 0,
        right: 0,
        margin: 10,
    },
    itemStatusSale: {
        display: 'inline-block',
        fontSize: 18,
        fontWeight: 500,
        backgroundColor: "#E2231A",
        color: "#fff",
        padding: 6,
        borderRadius: 5,
        lineHeight: "1em",
        marginLeft: 6,
    },
    itemStatusSaleIcon: {
        backgroundPosition: "-20px -720px",
        width: 20,
        height: 20,
        backgroundSize: "40px auto",
        "&:hover": {
            backgroundPosition: "-20px -720px",
        }
    },
    itemStatusSaleNew: {
        fontSize: 15,
        lineHeight: "1em",
        fontWeight: 500,
        marginLeft: 3,
        color: "#fff",
    },
    saleClockContainer: {
        fontSize: 14,
        color: "#191919",
        textAlign: "center",
        fontWeight: 300,
        lineHeight: '1.4em',
        // marginTop: 12,
        position: "absolute",
        width: 180,
    },
    saleClock: {
        fontSize: 14,
        color: "#191919",
        textAlign: "center",
        fontWeight: 300,
        lineHeight: '1.4em',
        "& span": {
            color: "#E2231A",
            marginLeft: 3,
        },
    },
    dropdownContainer: {
        position: "absolute",
        top: 0,
        right: 0,
        display: "flex",
    },
    offsaleLabel: {
        width: "calc(100% - 180px)!important",
        color: "#191919",
        fontWeight: 400,
        lineHeight: "1.4em",
        fontSize: 16,
        paddingRight: 12,
    },
    availableInventoryLabel: {
        width: "calc(100% - 180px)!important",
        color: "#191919",
        fontWeight: 500,
        lineHeight: "1.4em",
        fontSize: 16,
    },
    bannerAdContainer: {
        maxHeight: 90,
        marginBottom: 15,
    },
    skyscraperAdContainer: {
        maxWidth: 160,
    },
    ownedStat: {
        verticalAlign: 'text-bottom',
        display: 'inline-block',
        fontSize: '10px',
        backgroundColor: 'var(--success-color)',
        color: '#fff',
        padding: '3px',
        borderRadius: '50%',
        lineHeight: 1,
        aspectRatio: "1 / 1",
        height: "100%",
        width: "auto",
        marginLeft: 12,
        marginRight: 3,
    },
});

/**
 * @param {AssetDetailsEntry} itemDetails
 * @returns {JSX.Element}
 * @constructor
 */
function AssetDetailsPage({ itemDetails }) {
    const s = useStyles();
    const store = AssetDetailsStore.useContainer();
    const modal = AssetDetailsModalStore.useContainer();
    const auth = Authentication.useContainer();
    const buttonStyles = useButtonStyles();
    const [detailOptions, setDetailOptions] = useState(/** @type DetailOptionEntry[] */([]));
    const [isNew, setNew] = useState(false);
    
    useEffect(async () => {
        await store.setDetails(itemDetails);
        await setNew(IsISOWithinDays(itemDetails.createdAt, 3));
        if (!store.details || !store.resellers) return;
        let purchaseInfo = store.getPurchaseInfo();
        await setDetailOptions([
            (itemDetails.itemRestrictions.includes("Limited") || itemDetails.itemRestrictions.includes("LimitedUnique")) && !itemDetails.isForSale && store.resellers.length > 0 && (purchaseInfo.sellerId === auth.userId) ? {
                label: "Best Price",
                field: <div className={s.priceWrapper}>
                    <div className={`flex flex-column justify-content-between`} style={{ marginRight: 12, }}>
                        <div className={`${s.priceContainer} flex`}>
                            <span className={`icon-robux ${s.priceIcon}`}/>
                            <span className={s.priceLabel}
                                  style={{ color: "var(--robux-color)" }}>{formatNum(purchaseInfo?.price || 0)}</span>
                        </div>
                        <span style={{ fontSize: 12, fontWeight: 300 }}>You are selling this item.</span>
                    </div>
                    <ActionButton className={s.buyBtn} label="Buy" buttonStyle={buttonStyles.newBuyButton}
                                  disabled={true}/>
                </div>,
            } : (itemDetails.itemRestrictions.includes("Limited") || itemDetails.itemRestrictions.includes("LimitedUnique")) && !itemDetails.isForSale && store.resellers.length > 0 && (store.resellers.length !== 1 || store.resellers[0].seller.id !== auth.userId) ? {
                label: "Best Price",
                field: <div className={s.priceWrapper}>
                    <div className={`flex flex-column justify-content-between`} style={{ marginRight: 12, }}>
                        <div className={`${s.priceContainer} flex`}>
                            <span className={`icon-robux ${s.priceIcon}`}/>
                            <span className={s.priceLabel}
                                  style={{ color: "var(--robux-color)" }}>{formatNum(purchaseInfo?.price || 0)}</span>
                        </div>
                        <span style={{ fontSize: 12, fontWeight: 400 }}>See more <a className={`link2018`} style={{fontSize:"inherit",fontWeight:"inherit"}} onClick={() => {
                            document.getElementById("itemResellerHeader").scrollIntoView({ behavior: "smooth" });
                        }}>Resellers</a></span>
                    </div>
                    <ActionButton className={s.buyBtn} label="Buy" buttonStyle={buttonStyles.newBuyButton}
                                  onClick={() => modal.setBuyModalOpen(true)}/>
                </div>,
            } : (itemDetails.itemRestrictions.includes("Limited") || itemDetails.itemRestrictions.includes("LimitedUnique")) && !itemDetails.isForSale ? {
                label: "No one is selling this item currently.",
                field: <div>
                    <ActionButton className={s.buyBtn} label="Buy" buttonStyle={buttonStyles.newBuyButton}
                                  disabled={true}/>
                </div>,
                labelClass: s.offsaleLabel,
            } : store.isOwned && AssetTypeCategory.All.includes(itemDetails.assetType) ? {
                label: "This item is available in your inventory.",
                field: <div className={s.editWrapper}>
                    <Link href={getAvPageStyle() === avPageStyleType.Legacy ? '/My/Character.aspx' : '/My/Avatar'}>
                        <a href={getAvPageStyle() === avPageStyleType.Legacy ? '/My/Character.aspx' : '/My/Avatar'}>
                            <ActionButton
                                className={`${s.editBtn} ${s.buyBtn}`}
                                divClassName={s.buyBtnContainer}
                                label="Edit Avatar"
                                buttonStyle={buttonStyles.newCancelButton}
                            />
                        </a>
                    </Link>
                </div>,
                labelClass: s.availableInventoryLabel,
            } : itemDetails.isForSale ? {
                label: "Price",
                field: <div className={s.priceWrapper}>
                    <div className={`${s.priceContainer} flex`}>
                        {
                            itemDetails.priceTickets
                            ?
                            <>
                                <span className={`icon-tix ${s.priceIcon}`}/>
                                <span className={s.priceLabel}
                                      style={{ color: "var(--tix-color)" }}>{formatNum(itemDetails.priceTickets)}</span>
                            </>
                            :
                            <>
                                <span className={`icon-robux ${s.priceIcon}`}/>
                                <span className={s.priceLabel}
                                      style={{ color: "var(--robux-color)" }}>{formatNum(itemDetails.price)}</span>
                            </>
                        }
                    </div>
                    <ActionButton className={s.buyBtn} divClassName={s.buyBtnContainer} divChildren={
                        itemDetails.offsaleDeadline
                        ?
                        <div className={`${s.saleClockContainer}`} offsaleBy={itemDetails.offsaleDeadline}>
                            Offsale in
                            <Countdown className={s.saleClock} timestamp={itemDetails.offsaleDeadline}/>
                        </div>
                        :
                        null
                    } label="Buy" buttonStyle={buttonStyles.newBuyButton} onClick={() => modal.setBuyModalOpen(true)}/>
                </div>,
            } : {
                label: "This item is not currently for sale.",
                field: <div>
                    <ActionButton className={s.buyBtn} label="Buy" buttonStyle={buttonStyles.newBuyButton} disabled={true}/>
                </div>,
                labelClass: s.offsaleLabel,
            },
            {
                label: "Type",
                field: getTypeStrFromTypeNum(itemDetails.assetType),
            },
            {
                label: "Sales",
                field: formatNum(itemDetails.saleCount),
            },
            {
                label: "Genres",
                field: <div>
                    {
                        itemDetails.genres.map((genre, ind) => <>
                            <Link href={`/catalog?genres=${genresList.find(d => d.name === genre).genre}`}>
                                <a
                                    href={`/catalog?genres=${genresList.find(d => d.name === genre).genre}`}
                                    className={`${s.genreLabel} link2018`}
                                >{genre}</a>
                            </Link>
                            {
                                itemDetails.genres.length - 1 !== ind ? <span>, </span> : null
                            }
                        </>)
                    }
                </div>,
            },
            // itemDetails.assetType === AssetType.Gear ? {
            //     label: "Attributes",
            //     field: <span>
            //         {
            //             itemDetails.genres.map(genre => <>
            //                 <Link href={`/catalog?genres=${genresList.find(d => d.name === genre).genre}`}>
            //                     <a
            //                         href={`/catalog?genres=${genresList.find(d => d.name === genre).genre}`}
            //                         className={s.genreLabel}
            //                     >{genre}</a>
            //                 </Link>
            //                 <span>, </span>
            //             </>)
            //         }
            //     </span>,
            // } : null,
            {
                label: "Description",
                field: <pre className={s.itemDescription}>{itemDetails.description?.replace(/\\n/g, "\n") || "No description available"}</pre>,
            },
        ]);
    }, [itemDetails, store.details, store.resellers, store.isOwned]);
    
    if (!itemDetails || !store.details) {
        return <span className="spinner" style={{ height: "100%", backgroundSize: "auto 36px" }}/>
    }
    
    return <div className={s.container}>
        { modal.isBuyModalOpen ? <BuyModal /> : null }
        { modal.isSellItemModalOpen ? <SellItemModal /> : null }
        { modal.isConfirmSellModalOpen ? <ConfirmSellModal /> : null }
        <div className={`section-content noShadow ${s.itemContainer}`}>
            <div className={`${s.itemThumbContainer} flex flex-column`}>
                <div className={`w-fit-content position-relative ${itemDetails.assetType === AssetType.Badge || itemDetails.assetType === AssetType.GamePass ? s.smallImg : ""}`}>
                    <ItemImage name={itemDetails.name} id={itemDetails.id} className={`${s.img}`}/>
                    <div className={s.itemStatusContainer}>
                        {
                            isNew
                            ?
                            <div className={s.itemStatusSale}>
                                <span className={`${s.itemStatusSaleIcon} icon-clock`}/>
                                <span className={s.itemStatusSaleNew}>New</span>
                            </div>
                            :
                            null
                        }
                        {
                            itemDetails.offsaleDeadline
                            ?
                            <div className={s.itemStatusSale}>
                                <span className={`${s.itemStatusSaleIcon} icon-clock`}/>
                            </div>
                            :
                            null
                        }
                    </div>
                    <div className={s.restrictionsContainer}>
                        {
                            itemDetails.itemRestrictions.includes("LimitedUnique")
                            ?
                            <span className="icon-labels LimitedUnique"/>
                            :
                            itemDetails.itemRestrictions.includes("Limited")
                            ?
                            <span className="icon-labels Limited"/>
                            :
                            null
                        }
                    </div>
                    {
                        itemDetails.assetType === AssetType.Badge || itemDetails.assetType === AssetType.GamePass
                        ?
                        <RelatedGame />
                        :
                        null
                    }
                    {
                        itemDetails.assetType === AssetType.Audio
                        ?
                        <AudioPlayButton audioId={itemDetails.id} />
                        :
                        null
                    }
                </div>
                <div className={s.itemInteractionContainer}>
                    <div className="flex" style={{ marginTop: 6, }}>
                        <FavouriteButton assetId={itemDetails.id} initFavCount={itemDetails.favoriteCount} />
                    </div>
                </div>
            </div>
            <div className={s.itemDetailsContainer}>
                <div className={`${s.itemHeaderContainer} flex w-100 flex-column`}>
                    <h2>{itemDetails.name}</h2>
                    <div className="flex align-items-center">
                        <span style={{ color: "var(--text-color-secondary)" }}>By <CreatorLink
                            type={itemDetails.creatorType} id={itemDetails.creatorTargetId}
                            name={itemDetails.creatorName}/></span>
                        {
                            store.isOwned ? <>
                                <div className={s.ownedStat}>
                                    <span className='icon-checkmark-white-hold'/>
                                </div>
                                <span style={{ fontWeight: 500, fontSize: 14 }}>Item Owned {store.isResellable() && store.ownedCopies.length > 0 ? `(${store.ownedCopies.length})` : ""}</span>
                            </> : null
                        }
                    </div>
                    <div className={s.dropdownContainer}>
                        <AssetDropdown/>
                    </div>
                </div>
                <div className={`${s.itemDetails} flex w-100 flex-column`}>
                    {
                        detailOptions.map((item, ind) => {
                            // are we first, and if not, is previous one price?
                            const isAfterBuy =
                                detailOptions[0] !== item && detailOptions[ind - 1].label.includes("Price");
                            return <div className={`${isAfterBuy ? "margin-none" : ''} ${s.attrContainer}`}>
                                <span
                                    className={`${!item.labelClass ? s.attrLabel : item.labelClass}`}
                                >{item.label}</span>
                                {
                                    typeof item.field === 'string'
                                    ?
                                    <span>{item.field}</span>
                                    :
                                    item.field
                                }
                            </div>
                        })
                    }
                    {
                        detailOptions.length === 0
                        ?
                        <span className="spinner" style={{ backgroundSize: "auto 36px" }}/>
                        :
                        null
                    }
                </div>
            </div>
        </div>
        {
            store.isResellable() ? <>
                <PriceChart />
                <Resellers />
                <Owners />
            </> : null
        }
        <AssetRecommendations />
    </div>
}

export default function DetailsPageContainer({ details }) {
    const s = useStyles();
    
    return <Theme2016>
        <div className="container big">
            <UserAdvertisement type={UserAdvertisementType.Banner728x90} wrapperClass={s.bannerAdContainer} />
            <div className={s.pageWrapper}>
                <AssetDetailsPage itemDetails={details}/>
                <UserAdvertisement type={UserAdvertisementType.SkyScraper160x600} wrapperClass={s.skyscraperAdContainer} backupWidth="160px" />
            </div>
        </div>
    </Theme2016>
}

/**
 * @param {number} type
 * @param {boolean} [simple]
 * @returns {string}
 */
export function getTypeStrFromTypeNum(type, simple = false) {
    switch (type) {
        case AssetType.Hat:
            return (simple ? "" : "Accessory | ") + "Hat";
        case AssetType.HairAccessory:
            return (simple ? "" : "Accessory | ") + "Hair";
        case AssetType.FaceAccessory:
            return (simple ? "" : "Accessory | ") + "Face";
        case AssetType.NeckAccessory:
            return (simple ? "" : "Accessory | ") + "Neck";
        case AssetType.ShoulderAccessory:
            return (simple ? "" : "Accessory | ") + "Shoulder";
        case AssetType.FrontAccessory:
            return (simple ? "" : "Accessory | ") + "Front";
        case AssetType.BackAccessory:
            return (simple ? "" : "Accessory | ") + "Back";
        case AssetType.WaistAccessory:
            return (simple ? "" : "Accessory | ") + "Waist";
        case AssetType.Animation:
            return "Animation";
        case AssetType.ClimbAnimation:
            return "Climb Animation";
        case AssetType.DeathAnimation:
            return "Death Animation";
        case AssetType.FallAnimation:
            return "Fall Animation";
        case AssetType.IdleAnimation:
            return "Idle Animation";
        case AssetType.JumpAnimation:
            return "Jump Animation";
        case AssetType.RunAnimation:
            return "Run Animation";
        case AssetType.SwimAnimation:
            return "Swim Animation";
        case AssetType.WalkAnimation:
            return "Walk Animation";
        case AssetType.PoseAnimation:
            return "Pose Animation";
        case AssetType.EmoteAnimation:
            return "Emote";
        case AssetType.Shirt:
            return "Shirt";
        case AssetType.Pants:
            return "Pants";
        case AssetType.TeeShirt:
        case AssetType.TShirt:
            return "T-Shirt";
        case AssetType.Model:
            return "Model";
        case AssetType.Plugin:
            return "Plugin";
        case AssetType.Mesh:
            return "Mesh";
        case AssetType.MeshPart:
            return "Mesh Part";
        case AssetType.Decal:
            return "Decal";
        case AssetType.Image:
            return "Image";
        case AssetType.Audio:
            return "Audio";
        case AssetType.Video:
            return "Video";
        case AssetType.Package:
            return "Package";
        case AssetType.GamePass:
            return "Game Pass";
        case AssetType.Badge:
            return "Badge";
        case AssetType.Gear:
            return "Gear";
        case AssetType.Place:
            return "Place";
        case AssetType.SolidModel:
            return "Solid Model";
        case AssetType.Head:
            return "Head";
        case AssetType.Torso:
            return "Torso";
        case AssetType.LeftArm:
            return "Left Arm";
        case AssetType.LeftLeg:
            return "Left Leg";
        case AssetType.RightArm:
            return "Right Arm";
        case AssetType.RightLeg:
            return "Right Leg";
        case AssetType.Face:
            return "Face";
        default:
            return "Unknown";
    }
}

/**
 * @param {string} iso
 * @param {number} days
 * @returns {boolean}
 */
function IsISOWithinDays(iso, days) {
    iso = iso.replace(/\.\d{6}Z$/, "Z");
    const then = new Date(iso);
    const now = new Date();
    const dayNum = days * 24 * 60 * 60 * 1000;
    return (now - then) < dayNum;
}

/**
 * @param {number|string} x
 * @returns {string}
 */
export function formatNum(x) {
    return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

/**
 * @typedef DetailOptionEntry
 * @property {string|JSX.Element} label
 * @property {string|JSX.Element} field
 * @property {string?} labelClass
 */

