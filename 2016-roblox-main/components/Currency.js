import {createUseStyles} from "react-jss";
import React, { useEffect, useState } from "react";
import { formatNum } from "./assetDetailsPage";
import { CurrencySize, CurrencyType, GetEnum } from "../models/enums";

const useStyles = createUseStyles({
    priceIcon: { marginRight: 3, },
    priceLabel: {
        lineHeight: "1.16em",
        fontSize: 20,
        fontWeight: 700,
        marginTop: 2,
    },
});

/**
 * @param {CurrencyType} currencyType
 * @param {number|string} price
 * @param {boolean} [formatted]
 * @param {CurrencySize} [size]
 * @param {string} [divClass]
 * @param {string} [iconClass]
 * @param {string} [labelClass]
 * @param {boolean} [grayed]
 * @returns {JSX.Element}
 * @constructor
 */
function Currency({ currencyType, price,
                      formatted = true,
                      size = CurrencySize["28x28"],
                      divClass = "",
                      iconClass = "",
                      labelClass = "",
                      grayed,
}) {
    const s = useStyles();
    const [currencyColor, setCurrencyColor] = useState("var(--robux-color)");
    const [currencyIcon, setCurrencyIcon] = useState("icon-robux");
    
    useEffect(() => {
        setCurrencyColor(`var(--${currencyType === CurrencyType.Tickets ? "tix" : "robux"}-color)`);
        let classCurrency = currencyType === CurrencyType.Tickets ? "tix" : "robux";
        let classSize = size === CurrencySize["28x28"] ? "" : `-${GetEnum(CurrencySize, size)}`;
        let classGray = grayed && currencyType === CurrencyType.Robux ? "-gray" : "";
        setCurrencyIcon(`icon-${classCurrency}${classGray}${classSize}`);
    }, [currencyType, size, grayed]);
    
    return <div className={`${s.priceContainer} flex ${divClass}`}>
        <span className={`${currencyIcon} ${s.priceIcon} ${iconClass}`}/>
        <span className={`${s.priceLabel} ${labelClass}`}
              style={{ color: grayed ? "#b8b8b8" : currencyColor }}>{formatted ? formatNum(price) : price}</span>
    </div>
}

export default Currency;
