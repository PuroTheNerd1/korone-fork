import {createUseStyles} from "react-jss";
import Link from "../../link";
import AvatarPageStore from "../stores/avatarPageStore";
import AvatarInfoStore from "../stores/avatarInfoStore";
import ActionButton from "../../actionButton";
import {useRef} from "react";
import {wait} from "../../../lib/utils";

const useAvCardStyles = createUseStyles({
    avatarCardWrapper: {
        borderRadius: 3,
        width: "20%",
        padding: 5,
        display: "flex",
        flexDirection: "column",
    },
    avatarCardContainer: {
        width: 126,
        backgroundColor: "#fff",
        position: "relative",
        boxShadow: "0 1px 4px 0 rgba(25,25,25,0.3)",
        borderRadius: 3,
        maxWidth: 150,
        transition: "box-shadow 200ms ease",
        "-webkit-transition": "box-shadow 200ms ease",
        "&:hover": {
            boxShadow: "0 1px 6px 0 rgba(25,25,25,0.75)",
        }
    },
    avatarCardImage: {
        cursor: "pointer",
        width: "126px",
        height: "126px",
        borderTopLeftRadius: 3,
        borderTopRightRadius: 3,
        borderBottom: "1px solid #e3e3e3",
        "& img": {
            width: "100%",
            minHeight: "100%",
            height: "auto",
            borderTopLeftRadius: 3,
            borderTopRightRadius: 3,
            minWidth: "85px",
        }
    },
    avatarCardItemLink: {
        paddingTop: 6,
        lineHeight: "16px",
        width: "100%",
        padding: "6px 6px 0 6px",
        display: "inline-block",
        "& span": {
            height: "20px",
            lineHeight: "16px",
            display: "inline-block",
            maxWidth: '100%',
            fontSize: 16,
            padding: 0,
        }
    },
    avatarCardEquipped: {
        borderRadius: 3,
        pointerEvents: "none",
        border: "2px solid #02b757",
        position: "absolute",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        "& span": {
            width: 0,
            height: 0,
            borderTop: "36px solid #02b757",
            borderLeft: "36px solid transparent",
            position: "absolute",
            top: 0,
            right: 0,
        }
    },
});

function ThumbnailFromState(thumbnail, state) {
    switch (state.toLowerCase()) {
        case "pending":
            return "/img/placeholder.png";
        case "blocked":
            return "/img/blocked.png";
        case "completed":
            return thumbnail;
        default:
            return "/img/error.png";
    }
}

/**
 * @param {SortedItem} asset
 * @returns {JSX.Element}
 * @constructor
 */
function AvatarCard({ asset }) {
    const s = useAvCardStyles();
    const store = AvatarInfoStore.useContainer();
    const isEquipped = store?.wearingAssets?.length && store.wearingAssets.map(d => d.assetId).includes(asset.assetId);
    
    return <div className={`${s.avatarCardWrapper}`}>
        <div className={`${s.avatarCardContainer}`}>
            <div className={s.avatarCardImage}>
                <img
                    src={ThumbnailFromState(asset.thumbnail, asset.thumbnailState)} alt={asset.name}/>
            </div>
            <Link href={`/catalog/${asset.assetId}/${encodeURIComponent(asset.name)}`}>
                <a className={s.avatarCardItemLink} href={`/catalog/${asset.assetId}/${encodeURIComponent(asset.name)}`}>
                    <span className='text-overflow'>{asset.name}</span>
                </a>
            </Link>
            {
                isEquipped && <div className={s.avatarCardEquipped}>
                    <span></span>
                </div>
            }
        </div>
    </div>
}

function AvatarCardList() {
    const page = AvatarPageStore.useContainer();
    const deb = useRef(false);
    
    return <div className={`flex`}>
        {
            page.listItems.map(item =>
                <AvatarCard asset={item} />
            )
        }
        {
            page?.listItemMetadata?.nextPageCursor && page?.listItemMetadata?.assetType &&
            <div>
                <ActionButton label="Load More" onClick={async () => {
                    if (deb) return;
                    deb.current = true;
                    await page.LoadAssetTypeToList(page.listItemMetadata.assetType);
                    await wait(2.5);
                    deb.current = false;
                }} />
            </div>
        }
    </div>
}

export default AvatarCardList;
