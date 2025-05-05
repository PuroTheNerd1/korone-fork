import {createUseStyles} from "react-jss";
import AvatarInfoStore from "./stores/avatarInfoStore";
import AuthenticationStore from "../../stores/authentication";
import FeedbackStore from "../../stores/feedback";
import ActionButton from "../actionButton";
import useButtonStyles from "../../styles/buttonStyles";
import AvatarCardList from "./components/avatarCardList";
import RadioPill from "../radioPill";
import Slider from "../slider";
import HorizontalTabs from "../horizontalTabs";
import AvatarTabSubmenu, {SUBMENU_MODE} from "./components/avatarTabSubmenu";
import AvatarPageStore from "./stores/avatarPageStore";
import AvatarTabs from "./components/avatarTabs";
import {IsNullOrEmpty} from "../../lib/utils";

const useStyles = createUseStyles({
    sliderInput: {
        width: "100%",
    },
    avatarHeader: {},
    avatarThumbContainer: {
        position: "relative",
        backgroundImage: "url(/img/avatar-background.svg)",
        backgroundSize: "352px 352px",
        overflow: "hidden",
        height: 352,
        width: 277,
        "& img": {
            width: 352,
            height: "100%",
            verticalAlign: "middle",
            opacity: 1,
            transition: "opacity .5s ease",
            position: "absolute",
            top: 18,
            right: "-37.5px",
            userSelect: "none",
        }
    },
    avatarRigTypeSelector: {
        position: "absolute",
        right: 10,
        top: 10,
    },
    itemContainer: {
        flex: 1,
    },
    moreBut: {
        padding: 9,
        fontSize: 18,
        margin: 0,
    },
    iconDown: {
        backgroundPosition: "0 -204px",
        width: 12,
        height: 12,
        backgroundSize: "24px auto",
        bottom: "2px",
        position: "relative",
        marginLeft: "6px"
    },
    redrawContainer: {
        "& span": {
            fontSize: 16,
        }
    },
    redrawBtn: {
        padding: 4,
        fontSize: 14,
        lineHeight: "100%",
    },
});

// REF: https://youtu.be/iXI3aut2UWs
// REF2: https://youtu.be/pF-jlI9OJGs

function AvatarEditor() {
    const s = useStyles();
    const buttonStyles = useButtonStyles();
    const auth = AuthenticationStore.useContainer();
    const store = AvatarInfoStore.useContainer();
    const page = AvatarPageStore.useContainer();
    
    /**
     * @param {SubmenuData} item
     * @param {FormEvent<HTMLDivElement>} e
     * @constructor
     */
    async function AssetTypeClick(item, e) {
        page.setSelectedList({
            tab: page.openSubmenu.name,
            subTab: item.name,
        });
        await page.LoadAssetTypeToList(item.typeId);
    }
    
    /**
     * @param {SubmenuData} item
     * @param {FormEvent<HTMLDivElement>} e
     * @constructor
     */
    async function RecentClick(item, e) {
        page.setSelectedList({
            tab: page.openSubmenu.name,
            subTab: item.name,
        });
        await page.LoadRecentItemsToList(item.typeId);
    }
    
    return <div>
        <div className={`${s.avatarHeader} flex justify-content-between align-items-center`}>
            <h1 style={{ fontSize: 36, fontWeight: 500, padding: "15px 0", margin: 0, }}>Avatar Editor</h1>
            <div className="flex justify-content-center align-items-center" style={{ gap: 12 }}>
                <span>Explore the catalog to find more clothes!</span>
                <ActionButton label="Get More" className={s.moreBut} buttonStyle={buttonStyles.newBuyButton} onClick={() => {
                    window.location.href = "/catalog"
                }}/>
            </div>
        </div>
        <div className="flex" style={{ gap: 15 }}>
            <div>
                <div className="section-content" style={{ padding: 0 }}>
                    <div className={s.avatarThumbContainer}>
                        {
                            store?.avThumb ?
                                <img src={store.avThumb} alt={`${auth.username}'s Avatar`}/>
                                : !store.isRendering ?
                                    <img src="/img/placeholder-t.png" alt={`U`}/>
                                    : <span className="spinner" style={{height: "100%", backgroundSize: "auto 36px"}}/>
                        }
                        <div className={s.avatarRigTypeSelector}>
                            <RadioPill options={[
                                "R6",
                                "R15"
                            ]} selected={store?.bodyRigType} setSelected={store?.setBodyRigType} />
                        </div>
                    </div>
                    <div className={s.scalingContainer} style={{ padding: 15, paddingTop: 0 }}>
                        <div>
                            <h1 style={{ textAlign: 'start', fontSize: 21, padding: "15px 0px 13px 0", margin: 0, }}>Scaling</h1>
                        </div>
                        <div>
                            {
                                store?.avRules && store?.bodyScales &&
                                Object.entries(store.avRules.scales).map(([key, value]) => (
                                    <>
                                        <div style={{color: store.bodyRigType === "R6" ? "#b8b8b8" : "var(--text-color-primary)"}} className="flex justify-content-between">
                                            <span style={{color: 'inherit'}}>{CapitalizeVariable(key)}</span>
                                            <span>{Math.round(store.bodyScales[key] * 100)}%</span>
                                        </div>
                                        <Slider
                                            className={s.sliderInput}
                                            min={value.min}
                                            max={value.max}
                                            step={value.increment * 5}
                                            value={store.bodyScales[key]}
                                            setValue={(val) => {
                                                store.setBodyScales(prev => ({...prev, [key]: val.target.value}));
                                            }}
                                            disabled={store.bodyRigType === "R6"}
                                        />
                                    </>
                                ))
                            }
                        </div>
                    </div>
                </div>
                <div className={`flex justify-content-between ${s.redrawContainer}`}>
                    <span>Avatar isn't loading correctly?</span>
                    <ActionButton onClick={async () => {
                        await store.ForceRender();
                    }} label="Redraw" buttonStyle={buttonStyles.newCancelButton} className={s.redrawBtn} />
                </div>
            </div>
            <div className={s.itemContainer}>
                <AvatarTabs
                    options={[
                        {
                            id: "recent",
                            name: <span style={{ fontSize: 18 }}>Recent <span className={`icon-down ${s.iconDown}`} /></span>,
                            element: <AvatarTabSubmenu
                                data={[
                                    {
                                        name: "All",
                                        typeId: "all",
                                    },
                                    {
                                        name: "Clothing",
                                        typeId: "clothing",
                                    },
                                    {
                                        name: "Body Parts",
                                        typeId: "bodyparts",
                                    },
                                    {
                                        name: "Animations",
                                        typeId: "avataranimations",
                                    },
                                    {
                                        name: "Accessories",
                                        typeId: "accessories",
                                    },
                                    {
                                        name: "Outfits",
                                        typeId: "outfits",
                                    },
                                ]}
                                onButtonClick={RecentClick}
                            />
                        },
                        {
                            id: "clothing",
                            name: <span style={{ fontSize: 18 }}>Clothing <span className={`icon-down ${s.iconDown}`} /></span>,
                            element: <AvatarTabSubmenu
                                data={[
                                    {
                                        label: "Accessories",
                                        items: [
                                            {
                                                name: "Hat",
                                                typeId: 8,
                                            },
                                            {
                                                name: "Hair",
                                                typeId: 41,
                                            },
                                            {
                                                name: "Face",
                                                typeId: 42,
                                            },
                                            {
                                                name: "Neck",
                                                typeId: 43,
                                            },
                                            {
                                                name: "Shoulders",
                                                typeId: 44,
                                            },
                                            {
                                                name: "Front",
                                                typeId: 45,
                                            },
                                            {
                                                name: "Back",
                                                typeId: 46,
                                            },
                                            {
                                                name: "Waist",
                                                typeId: 47,
                                            },
                                        ],
                                    },
                                    {
                                        label: "Clothes",
                                        items: [
                                            {
                                                name: "Shirts",
                                                typeId: 11,
                                            },
                                            {
                                                name: "Pants",
                                                typeId: 12,
                                            },
                                            {
                                                name: "T-Shirts",
                                                typeId: 2,
                                            },
                                        ],
                                    },
                                    {
                                        label: "Gear",
                                        items: [{name: "Gear", typeId: 19}],
                                    },
                                ]}
                                onButtonClick={AssetTypeClick}
                                mode={SUBMENU_MODE.NESTED}
                            />
                        },
                        {
                            id: "body",
                            name: <span style={{ fontSize: 18 }}>Body <span className={`icon-down ${s.iconDown}`} /></span>,
                            element: <AvatarTabSubmenu
                                data={[
                                    {
                                        name: "Skin Tone",
                                        typeId: 0,
                                    },
                                    {
                                        name: "Packages",
                                        typeId: 32,
                                    },
                                    {
                                        name: "Face",
                                        typeId: 18,
                                    },
                                    {
                                        name: "Head",
                                        typeId: 17,
                                    },
                                    {
                                        name: "Torso",
                                        typeId: 27,
                                    },
                                    {
                                        name: "Left Arms",
                                        typeId: 29,
                                    },
                                    {
                                        name: "Right Arms",
                                        typeId: 28,
                                    },
                                    {
                                        name: "Left Legs",
                                        typeId: 30,
                                    },
                                    {
                                        name: "Right Legs",
                                        typeId: 31,
                                    },
                                ]}
                                onButtonClick={AssetTypeClick}
                            />
                        },
                        {
                            id: "animations",
                            name: <span style={{ fontSize: 18 }}>Animations <span className={`icon-down ${s.iconDown}`} /></span>,
                            element: <AvatarTabSubmenu
                                data={[
                                    {
                                        name: "Walk",
                                        typeId: 55,
                                    },
                                    {
                                        name: "Run",
                                        typeId: 53,
                                    },
                                    {
                                        name: "Fall",
                                        typeId: 50,
                                    },
                                    {
                                        name: "Jump",
                                        typeId: 52,
                                    },
                                    {
                                        name: "Swim",
                                        typeId: 54,
                                    },
                                    {
                                        name: "Climb",
                                        typeId: 48,
                                    },
                                    {
                                        name: "Idle",
                                        typeId: 51,
                                    },
                                ]}
                                onButtonClick={AssetTypeClick}
                            />
                        },
                        {
                            id: "outfits",
                            name: <span style={{ fontSize: 18 }}>Outfits</span>,
                            element: null,
                            onClick: (v) => {
                                console.log("OUTFITS");
                            }
                        },
                    ]}
                    default={<span style={{fontSize: 18}}>Recent <span className={`icon-down ${s.iconDown}`}/></span>}
                />
                <div style={{ display: "flex" }}>
                    <span
                        style={{ paddingTop: 9, paddingBottom: 4, marginLeft: 5 }}
                    >{CapitalizeVariable(page.selectedList.tab)}
                        {!IsNullOrEmpty(page?.selectedList?.subTab) && ` > ${CapitalizeVariable(page?.selectedList?.subTab)}`}
                    </span>
                </div>
                <AvatarCardList />
            </div>
        </div>
    </div>
}

export function CapitalizeVariable(str) {
    console.log(str);
    return str
        ?.replace(/([A-Z])/g, " $1")
        ?.replace(/^./, str => str?.toUpperCase());
}

export default AvatarEditor;
