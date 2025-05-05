import React from "react";
import {createUseStyles} from "react-jss";

const useStyles = createUseStyles({
    submenuContainer: {
        padding: 12,
        borderTop: "1px solid #b8b8b8",
        opacity: 1,
        transition: "opacity 100ms",
        width: "100%",
        position: "absolute",
        zIndex: 1,
        margin: "0 0 18px",
    },
    submenuButton: {
        "-webkit-transition": "all 200ms ease",
        transition: "all 200ms ease",
        cursor: "pointer",
        borderRadius: 3,
        margin: "0 3px",
        padding: 7,
        float: "left",
        lineHeight: '100%',
        "&:hover": {
            color: "#fff",
            backgroundColor: "var(--primary-color)",
        }
    },
    submenuButtonActive: {
        color: "#fff",
        backgroundColor: "var(--primary-color)",
    },
    
    submenuNestContainer: {
        paddingTop: 12,
    },
    submenuNestLabel: {
        width: 90,
        marginRight: 12,
        padding: "7px 0",
        float: "left",
        borderRight: "1px solid #e3e3e3",
        color: "#b8b8b8",
        fontWeight: 400,
        fontSize: 16,
    },
    submenuColumn: {
        flexDirection: 'column',
    },
});

/**
 * @typedef SubmenuData
 * @property {string} name
 * @property {number} typeId
 * @property {bool} active
 */
/**
 * @typedef SubmenuNestedData
 * @property {string} label
 * @property {SubmenuData[]} items
 */
const example = {
    id: "BodyParts",
    name: "Body Parts",
    typeId: 32,
    active: false
}
const nested = [
    {
        label: "Accessories",
        items: [example],
    }
]

/**
 *
 * @param {number} mode
 * @param {SubmenuData[] | SubmenuNestedData[]} data
 * @param {any} onButtonClick
 * @returns {Element}
 * @constructor
 */
const AvatarSubmenu = ({ data, onButtonClick, mode = SUBMENU_MODE.DEFAULT }) => {
    const s = useStyles();
    
    return <div className={`${s.submenuContainer} section-content ${mode === SUBMENU_MODE.NESTED && s.submenuColumn}`}>
        {
            mode === SUBMENU_MODE.DEFAULT &&
            data.map(item =>
                <div
                    className={`${s.submenuButton} ${item.active && s.submenuButtonActive}`}
                    onClick={e => onButtonClick(item, e)}
                >
                    {item.name}
                </div>
            )
        }
        {
            mode === SUBMENU_MODE.NESTED &&
            data.map(item => {
                /** @type SubmenuNestedData */
                let nest = item;
                return <div className={s.submenuNestContainer}>
                    <span className={s.submenuNestLabel}>{nest.label}</span>
                    {
                        nest.items.map(item =>
                            <div
                                className={`${s.submenuButton} ${item.active && s.submenuButtonActive}`}
                                onClick={e => onButtonClick(item, e)}
                            >
                                {item.name}
                            </div>
                        )
                    }
                </div>
            })
        }
    </div>
}

export const SUBMENU_MODE = Object.freeze({
    DEFAULT: 0,
    NESTED: 1,
})

export default AvatarSubmenu;