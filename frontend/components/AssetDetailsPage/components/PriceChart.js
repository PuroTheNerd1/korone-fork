import { createUseStyles } from "react-jss";
import AssetDetailsStore from "../stores/AssetDetailsStore";
import React, { useEffect, useMemo, useState } from "react";
import { formatNum } from "../index";
import Highcharts from "highcharts";
import HighchartsReact from "highcharts-react-official";
import { CurrencyType } from "../../../models/enums";
import Currency from "../../Currency";

const TIMELINE_OPTIONS = [
    { label: "180 Days", value: 180 },
    { label: "90 Days", value: 90 },
    { label: "30 Days", value: 30 },
];

const useStyles = createUseStyles({
    containerHeader: {
        margin: "3px 0 6px",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        "& h3": {
            fontSize: 20,
            fontWeight: 700,
            margin: 0,
        },
    },
    priceChartStatsContainer: {
        display: "flex",
        justifyContent: "space-around",
        alignItems: "center",
        width: "100%",
        marginTop: 12,
        paddingTop: 12,
        borderTop: "1px solid #e3e3e3",
    },
    priceChartStat: {
        display: "flex",
        flexDirection: "column",
        gap: 9,
        justifyContent: "center",
        alignItems: "center",
    },
    priceChartTextLabel: {
        color: "#b8b8b8",
        fontWeight: 500,
    },
    timelineSelect: {
        fontSize: 14,
        padding: "4px 24px 4px 8px",
        borderRadius: 3,
        border: "1px solid #e3e3e3",
        cursor: "pointer",
        backgroundColor: "#fff",
        color: "var(--text-color-primary)",
        appearance: "auto",
    },
    timelineDropdownContainer: {
        display: "flex",
        justifyContent: "flex-end",
        marginBottom: 6,
    },
});

/**
 * @param {boolean} [isLabelHidden]
 * @returns {Element}
 * @constructor
 */
function PriceChart({ isLabelHidden = false }) {
    const s = useStyles();
    const store = AssetDetailsStore.useContainer();
    const [prices, setPrices] = useState({
        originalPriceCurrency: 1,
        originalPrice: -1,
        averagePrice: -1,
    });
    const [priceChartData, setPriceChartData] = useState([]);
    const [volumeChartData, setVolumeChartData] = useState([]);
    const [timelineDays, setTimelineDays] = useState(180);

    useEffect(() => {
        if (!store.resaleData) return;
        setPriceChartData(store.resaleData.priceDataPoints.map(d => [
            new Date(d.date).getTime(),
            d.value,
        ]));
        setVolumeChartData(store.resaleData.volumeDataPoints.map(d => [
            new Date(d.date).getTime(),
            d.value,
        ]));
    }, [store.resaleData]);

    useEffect(() => {
        if (!store.details || !store.resaleData) return;
        const currency = store.details?.priceTickets ? CurrencyType.Tickets : CurrencyType.Robux;
        const originalPrice = store.details?.priceTickets || store.details?.price || 0;
        const avgPrice = store.resaleData.recentAveragePrice;
        setPrices({
            originalPriceCurrency: currency,
            originalPrice: originalPrice,
            averagePrice: avgPrice,
        });
    }, [store.details, store.resaleData]);

    const filteredPriceData = useMemo(() => {
        const cutoff = Date.now() - timelineDays * 24 * 60 * 60 * 1000;
        return priceChartData.filter(([ts]) => ts >= cutoff);
    }, [priceChartData, timelineDays]);

    const filteredVolumeData = useMemo(() => {
        const cutoff = Date.now() - timelineDays * 24 * 60 * 60 * 1000;
        return volumeChartData.filter(([ts]) => ts >= cutoff);
    }, [volumeChartData, timelineDays]);

    const chartOptions = useMemo(() => ({
        chart: {
            style: {
                fontFamily: "HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif",
            },
            height: 200,
            backgroundColor: "transparent",
            margin: [20, 20, 40, 55],
        },
        title: { text: null },
        credits: { enabled: false },
        legend: {
            enabled: true,
            align: "left",
            verticalAlign: "top",
            itemStyle: {
                fontWeight: 500,
                fontSize: 12,
                color: "#888",
            },
            symbolRadius: 0,
            symbolHeight: 3,
            y: -12,
        },
        xAxis: {
            type: "datetime",
            labels: {
                formatter: function () {
                    return Highcharts.dateFormat("%m/%d", this.value);
                },
                style: { fontSize: 11, color: "#b8b8b8" },
            },
            lineColor: "#e3e3e3",
            tickColor: "#e3e3e3",
        },
        yAxis: [
            {
                title: { text: null },
                labels: {
                    formatter: function () {
                        if (this.value >= 1000) return (this.value / 1000) + "K";
                        return this.value;
                    },
                    style: { fontSize: 11, color: "#b8b8b8" },
                },
                gridLineColor: "#e3e3e3",
                min: 0,
            },
            {
                title: { text: null },
                labels: { enabled: false },
                gridLineWidth: 0,
                opposite: false,
                min: 0,
            },
        ],
        tooltip: {
            formatter: function () {
                return formatNum(this.y);
            },
            backgroundColor: "#444",
            style: { color: "#fff", fontSize: 14, fontWeight: "600" },
            borderWidth: 0,
            borderRadius: 4,
            shadow: false,
        },
        plotOptions: {
            line: {
                marker: {
                    enabled: false,
                    states: {
                        hover: {
                            enabled: true,
                            radius: 5,
                            fillColor: "#00a36c",
                            lineColor: "#fff",
                            lineWidth: 2,
                        },
                    },
                },
            },
            column: {
                borderWidth: 0,
                groupPadding: 0.05,
                pointPadding: 0,
            },
        },
        series: [
            {
                name: "Recent Average Price",
                type: "line",
                data: filteredPriceData,
                color: "#00a36c",
                lineWidth: 2,
                yAxis: 0,
            },
            {
                name: "Volume",
                type: "column",
                data: filteredVolumeData,
                color: "#c8c8c8",
                yAxis: 1,
            },
        ],
    }), [filteredPriceData, filteredVolumeData]);

    if (!store.resaleData?.sales) {
        return <div>
            {
                !isLabelHidden ? <div className={`flex ${s.containerHeader}`}>
                    <h3>Price Chart</h3>
                </div> : null
            }
            <div className="section-content">
                <span className="spinner" style={{ height: "100%", backgroundSize: "auto 36px" }}/>
            </div>
        </div>
    }

    return <div>
        {
            !isLabelHidden ? <div className={`flex ${s.containerHeader}`}>
                <h3>Price Chart</h3>
                <select
                    className={s.timelineSelect}
                    value={timelineDays}
                    onChange={e => setTimelineDays(Number(e.target.value))}
                >
                    {TIMELINE_OPTIONS.map(opt => (
                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                </select>
            </div> : <div className={s.timelineDropdownContainer}>
                <select
                    className={s.timelineSelect}
                    value={timelineDays}
                    onChange={e => setTimelineDays(Number(e.target.value))}
                >
                    {TIMELINE_OPTIONS.map(opt => (
                        <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                </select>
            </div>
        }
        <div className="section-content noShadow">
            <div className="flex flex-column w-100">
                <HighchartsReact highcharts={Highcharts} options={chartOptions}/>
            </div>
            <div className={s.priceChartStatsContainer}>
                <div className={s.priceChartStat}>
                    <span className={s.priceChartTextLabel}>Quantity Sold</span>
                    <span style={{ fontWeight: 500 }}>{store.resaleData.sales}</span>
                </div>
                <div className={s.priceChartStat}>
                    <span className={s.priceChartTextLabel}>Original Price</span>
                    <div className={`flex`}>
                        <Currency
                            canBeFree
                            price={prices.originalPrice}
                            currencyType={prices.originalPriceCurrency}
                        />
                    </div>
                </div>
                <div className={s.priceChartStat}>
                    <span className={s.priceChartTextLabel}>Average Price</span>
                    <div className={`flex`}>
                        <Currency
                            price={prices.averagePrice}
                            currencyType={CurrencyType.Robux}
                        />
                    </div>
                </div>
            </div>
        </div>
    </div>
}

export default PriceChart;
