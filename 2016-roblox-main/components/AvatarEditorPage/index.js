import {createUseStyles} from "react-jss";
import AvatarInfoStore from "./stores/avatarInfoStore";
import AuthenticationStore from "../../stores/authentication";
import FeedbackStore from "../../stores/feedback";
import ActionButton from "../actionButton";
import useButtonStyles from "../../styles/buttonStyles";
import AvatarCardList from "./components/avatarCardList";
import RadioPill from "../radioPill";
import Slider from "../slider";

const useStyles = createUseStyles({
    sliderInput: {
        width: "100%",
    },
    avatarHeader: {
        marginBottom: 10
    },
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
});

// REF: https://youtu.be/iXI3aut2UWs
// REF2: https://youtu.be/pF-jlI9OJGs

function AvatarEditor() {
    const s = useStyles();
    const buttonStyles = useButtonStyles();
    const auth = AuthenticationStore.useContainer();
    const feedback = FeedbackStore.useContainer();
    const store = AvatarInfoStore.useContainer();
    
    return <div>
        <div className={`${s.avatarHeader} flex justify-content-between align-items-center`}>
            <h1>Avatar Editor</h1>
            <div className="flex justify-content-center align-items-center" style={{ gap: 5 }}>
                <span>Explore the catalog to find more clothes!</span>
                <ActionButton label="Get More" buttonStyle={buttonStyles.newBuyButton} onClick={() => {
                    window.location.href = "/catalog"
                }}/>
            </div>
        </div>
        <div className="flex" style={{ gap: 10 }}>
            <div>
                <div className="section-content" style={{ padding: 0 }}>
                    <div className={s.avatarThumbContainer}>
                        {
                            store?.avThumb ?
                                <img src={store.avThumb} alt={`${auth.username}'s Avatar`}/>
                                : <span className="spinner" style={{ height: "100%" }} />
                        }
                        <div className={s.avatarRigTypeSelector}>
                            <RadioPill options={[
                                "R6",
                                "R15"
                            ]} selected={store?.bodyRigType} setSelected={store?.setBodyRigType} />
                        </div>
                    </div>
                    <div className={s.scalingContainer} style={{ padding: 15 }}>
                        <div>
                            <h3 style={{ textAlign: 'start' }}>Scaling</h3>
                        </div>
                        <div>
                            {
                                store?.avRules && store?.bodyScales &&
                                Object.entries(store.avRules.scales).map(([key, value]) => (
                                    <>
                                        <div className="flex justify-content-between">
                                            <span>{CapitalizeVariable(key)}</span>
                                            <span>{Math.round(store.bodyScales[key] * 100)}%</span>
                                        </div>
                                        <Slider
                                            className={s.sliderInput}
                                            min={value.min}
                                            max={value.max}
                                            step={value.increment * 5}
                                            value={store.bodyScales[key]}
                                            setValue={(val) => {
                                                console.log(val.target.value);
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
                <div className="flex justify-content-between">
                    <span>Avatar isn't loading correctly?</span>
                    <ActionButton label="Redraw" />
                </div>
            </div>
            <div className={s.itemContainer}>
                {
                    // put navbar here
                }
                <div>
                    <span>Recent {'>'} Clothing</span>
                </div>
                <AvatarCardList />
            </div>
        </div>
    </div>
}

function CapitalizeVariable(str) {
    return str
        .replace(/([A-Z])/g, " $1")
        .replace(/^./, str => str.toUpperCase());
}

export default AvatarEditor;
