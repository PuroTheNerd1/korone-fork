import { createUseStyles } from "react-jss";
import React, { useEffect, useMemo, useRef, useState } from "react";
import Highcharts from "highcharts";
import HighchartsReact from "highcharts-react-official";
import AssetDetailsStore from "../stores/AssetDetailsStore";
import { CurrencyType } from "../../../models/enums";
import Currency from "../../Currency";

const timelines = [
    { label: "30 Days", days: 30 },
    { label: "90 Days", days: 90 },
    { label: "180 Days", days: 180 },
];

const useStyles = createUseStyles({
    wrapper: {
        marginBottom: 15,
        background: "#fff",
        borderRadius: 4,
        overflow: "hidden",
        boxShadow: "0 1px 2px rgba(25,25,25,0.08)",
    },
    header: {
        background: "#e3e3e3",
        padding: "10px 15px",
        "& h3": {
            margin: 0,
            fontSize: 20,
            fontWeight: 700,
            color: "#191919",
        },
    },
    body: {
        padding: "12px 15px 6px",
    },
    topRow: {
        display: "flex",
        justifyContent: "space-between",
        alignItems: "flex-start",
        marginBottom: 8,
        flexWrap: "wrap",
        gap: 8,
    },
    legend: {
        display: "flex",
        alignItems: "center",
        gap: 16,
    },
    legendItem: {
        display: "flex",
        alignItems: "center",
        gap: 6,
        cursor: "pointer",
        userSelect: "none",
        fontSize: 14,
        fontWeight: 500,
        color: "#191919",
    },
    legendItemOff: {
        color: "#b8b8b8",
    },
    dash: {
        display: "inline-block",
        width: 18,
        height: 3,
        borderRadius: 2,
    },
    dashGreen: { background: "#02b757" },
    dashGrey: { background: "#b8b8b8" },
    dropdown: {
        position: "relative",
        width: 150,
        userSelect: "none",
    },
    dropdownButton: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        width: "100%",
        padding: "6px 12px",
        height: 32,
        border: "1px solid #c3c3c3",
        borderRadius: 4,
        background: "#fff",
        color: "#191919",
        fontSize: 14,
        fontWeight: 500,
        cursor: "pointer",
        "&:hover": { borderColor: "#898989" },
    },
    dropdownButtonOpen: {
        background: "#0161ac",
        borderColor: "#0161ac",
        color: "#fff",
        "&:hover": { borderColor: "#0161ac" },
    },
    caret: {
        width: 0,
        height: 0,
        marginLeft: 8,
        borderLeft: "5px solid transparent",
        borderRight: "5px solid transparent",
        borderTop: "5px solid currentColor",
    },
    dropdownList: {
        position: "absolute",
        top: "calc(100% + 2px)",
        left: 0,
        right: 0,
        background: "#fff",
        border: "1px solid #c3c3c3",
        borderRadius: 4,
        boxShadow: "0 2px 6px rgba(0,0,0,0.12)",
        zIndex: 5,
        overflow: "hidden",
    },
    dropdownOption: {
        padding: "8px 12px",
        fontSize: 14,
        fontWeight: 500,
        color: "#191919",
        cursor: "pointer",
        "&:hover": { background: "#f1f1f1" },
    },
    divider: {
        height: 1,
        background: "#e3e3e3",
        margin: "8px 0 12px",
    },
    stats: {
        display: "flex",
        justifyContent: "space-around",
        alignItems: "flex-start",
        padding: "4px 0 12px",
    },
    stat: {
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 8,
    },
    statLabel: {
        color: "#b8b8b8",
        fontSize: 14,
        fontWeight: 500,
    },
    statValue: {
        color: "#191919",
        fontSize: 16,
        fontWeight: 500,
    },
    spinnerWrap: {
        minHeight: 360,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
    },
});

function getChartOptions(priceData, volumeData, showPrice, showVolume) {
    return {
        chart: {
            height: 340,
            spacing: [10, 0, 0, 0],
            backgroundColor: "transparent",
            style: {
                fontFamily: "HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif",
            },
        },
        credits: { enabled: false },
        title: { text: undefined },
        legend: { enabled: false },
        xAxis: {
            type: "datetime",
            lineColor: "#e3e3e3",
            tickColor: "#e3e3e3",
            tickLength: 4,
            labels: {
                style: { color: "#193a5e", fontSize: "11px", fontWeight: "500" },
                format: "{value:%m/%d}",
            },
            crosshair: {
                width: 1,
                color: "rgba(2,183,87,0.25)",
            },
        },
        yAxis: [
            {
                title: { text: null },
                gridLineColor: "#ededed",
                tickAmount: 3,
                top: "0%",
                height: "72%",
                offset: 0,
                lineWidth: 0,
                labels: {
                    style: { color: "#193a5e", fontSize: "11px", fontWeight: "500" },
                    x: -4,
                    formatter: function () {
                        return this.value >= 1000 ? (this.value / 1000) + "K" : this.value;
                    },
                },
            },
            {
                title: { text: null },
                gridLineWidth: 0,
                top: "78%",
                height: "22%",
                offset: 0,
                lineWidth: 0,
                labels: { enabled: false },
            },
        ],
        tooltip: {
            useHTML: true,
            backgroundColor: "#4a4a4a",
            borderColor: "#4a4a4a",
            borderRadius: 4,
            borderWidth: 0,
            shadow: false,
            padding: 0,
            style: { color: "#fff" },
            formatter: function () {
                return `<div style="padding:6px 10px;color:#fff;font-weight:500;font-size:13px;">${Math.round(this.y).toLocaleString()}</div>`;
            },
        },
        plotOptions: {
            series: {
                animation: { duration: 400 },
                states: {
                    hover: { lineWidthPlus: 0, halo: { size: 0 } },
                    inactive: { opacity: 1 },
                },
                marker: {
                    enabled: false,
                    states: {
                        hover: { enabled: true, radius: 5, lineWidth: 2, lineColor: "#fff" },
                    },
                },
            },
            column: {
                borderWidth: 0,
                pointPadding: 0.05,
                groupPadding: 0.05,
                enableMouseTracking: false,
            },
            line: { lineWidth: 2 },
        },
        series: [
            {
                type: "line",
                name: "Recent Average Price",
                data: priceData,
                color: "#02b757",
                visible: showPrice,
                yAxis: 0,
                zIndex: 2,
            },
            {
                type: "column",
                name: "Volume",
                data: volumeData,
                color: "#b8b8b8",
                visible: showVolume,
                yAxis: 1,
                zIndex: 1,
            },
        ],
    };
}

function ResalePriceChart({ isLabelHidden = false }) {
    const s = useStyles();
    const store = AssetDetailsStore.useContainer();
    const [timelineIdx, setTimelineIdx] = useState(2);
    const [dropdownOpen, setDropdownOpen] = useState(false);
    const [showPrice, setShowPrice] = useState(true);
    const [showVolume, setShowVolume] = useState(true);
    const dropdownRef = useRef(null);

    useEffect(() => {
        if (!dropdownOpen) return;
        const onClick = e => {
            if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
                setDropdownOpen(false);
            }
        };
        document.addEventListener("mousedown", onClick);
        return () => document.removeEventListener("mousedown", onClick);
    }, [dropdownOpen]);

    const resale = store.resaleData;

    const { priceData, volumeData } = useMemo(() => {
        if (!resale) return { priceData: [], volumeData: [] };
        const cutoff = Date.now() - timelines[timelineIdx].days * 86400000;
        const toPoint = d => [new Date(d.date).getTime(), d.value];
        const byTime = (a, b) => a[0] - b[0];
        return {
            priceData: (resale.priceDataPoints || []).map(toPoint).sort(byTime).filter(p => p[0] >= cutoff),
            volumeData: (resale.volumeDataPoints || []).map(toPoint).sort(byTime).filter(p => p[0] >= cutoff),
        };
    }, [resale, timelineIdx]);

    const options = useMemo(
        () => getChartOptions(priceData, volumeData, showPrice, showVolume),
        [priceData, volumeData, showPrice, showVolume],
    );

    const originalCurrency = store.details?.priceTickets ? CurrencyType.Tickets : CurrencyType.Robux;
    const originalPrice = store.details?.priceTickets || store.details?.price || 0;
    const averagePrice = resale?.recentAveragePrice ?? 0;
    const quantitySold = resale?.sales ?? 0;

    const header = !isLabelHidden ? (
        <div className={s.header}>
            <h3>Price Chart</h3>
        </div>
    ) : null;

    if (!resale) {
        return (
            <div className={s.wrapper}>
                {header}
                <div className={s.spinnerWrap}>
                    <span className="spinner" style={{ height: "100%", backgroundSize: "auto 36px" }}/>
                </div>
            </div>
        );
    }

    return (
        <div className={s.wrapper}>
            {header}
            <div className={s.body}>
                <div className={s.topRow}>
                    <div className={s.legend}>
                        <div
                            className={`${s.legendItem} ${!showPrice ? s.legendItemOff : ""}`}
                            onClick={() => setShowPrice(v => !v)}
                        >
                            <span className={`${s.dash} ${showPrice ? s.dashGreen : s.dashGrey}`}/>
                            Recent Average Price
                        </div>
                        <div
                            className={`${s.legendItem} ${!showVolume ? s.legendItemOff : ""}`}
                            onClick={() => setShowVolume(v => !v)}
                        >
                            <span className={`${s.dash} ${s.dashGrey}`}/>
                            Volume
                        </div>
                    </div>
                    <div className={s.dropdown} ref={dropdownRef}>
                        <div
                            className={`${s.dropdownButton} ${dropdownOpen ? s.dropdownButtonOpen : ""}`}
                            onClick={() => setDropdownOpen(v => !v)}
                        >
                            <span>{timelines[timelineIdx].label}</span>
                            <span className={s.caret}/>
                        </div>
                        {dropdownOpen && (
                            <div className={s.dropdownList}>
                                {timelines.map((opt, i) => (
                                    <div
                                        key={opt.days}
                                        className={s.dropdownOption}
                                        onClick={() => {
                                            setTimelineIdx(i);
                                            setDropdownOpen(false);
                                        }}
                                    >
                                        {opt.label}
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
                <HighchartsReact highcharts={Highcharts} options={options}/>
                <div className={s.divider}/>
                <div className={s.stats}>
                    <div className={s.stat}>
                        <span className={s.statLabel}>Quantity Sold</span>
                        <span className={s.statValue}>{quantitySold.toLocaleString()}</span>
                    </div>
                    <div className={s.stat}>
                        <span className={s.statLabel}>Original Price</span>
                        <Currency canBeFree price={originalPrice} currencyType={originalCurrency}/>
                    </div>
                    <div className={s.stat}>
                        <span className={s.statLabel}>Average Price</span>
                        <Currency price={averagePrice} currencyType={CurrencyType.Robux}/>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default ResalePriceChart;
