import { createUseStyles } from "react-jss";
import AssetDetailsStore from "../stores/AssetDetailsStore";
import React, { useEffect, useState } from "react";
import { formatNum } from "../index";
import Highcharts from "highcharts";
import HighchartsReact from "highcharts-react-official";

const useStyles = createUseStyles({
    containerHeader: {
        margin: "3px 0 6px",
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
    priceIcon: {
        marginRight: 3,
        width: 20,
        height: 20,
        backgroundSize: "40px auto",
        backgroundPosition: "0 -80px",
    },
    priceLabel: {
        lineHeight: "1.4em",
        fontSize: 16,
        fontWeight: 500,
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
    const [priceChartData, setPriceChartData] = useState(([]));
    const [volumeChartData, setVolumeChartData] = useState(([]));
    const [chartPref, setChartPref] = useState({
        timeline: 0, // 0 = 180 days, 2 = 90, 3 = 30
    });
    
    useEffect(() => {
        if (!store.resaleData) return;
        setPriceChartData(store.resaleData.priceDataPoints.map(d => [
            new Date(d.date).getTime(),
            d.value,
        ]));
        setVolumeChartData(store.resaleData.volumeDataPoints);
    }, [store.resaleData]);
    
    if (!store.resaleData?.sales) {
        return <div>
            {
                !isLabelHidden ? <div className={`flex ${s.containerHeader}`}>
                    <h3 style={{ margin: 0, }}>Price Chart</h3>
                </div> : null
            }
            <div className={`section-content`}>
                <span className="spinner" style={{ height: "100%", backgroundSize: "auto 36px" }}/>
            </div>
        </div>
    }
    
    return <div>
        {
            !isLabelHidden ? <div className={`flex ${s.containerHeader}`}>
                <h3 style={{ margin: 0, }}>Price Chart</h3>
            </div> : null
        }
        <div className={`section-content noShadow`}>
            {/*<div className={`flex flex-column w-100`}>*/}
            {/*    <HighchartsReact highcharts={Highcharts} options={{*/}
            {/*        chart: {*/}
            {/*            type: "line",*/}
            {/*            style: {*/}
            {/*                fontFamily: "HCo Gotham SSm,Helvetica Neue,Helvetica,Arial,Lucida Grande,sans-serif",*/}
            {/*            },*/}
            {/*        },*/}
            {/*        xAxis: { type: "datetime" },*/}
            {/*        series: [{ data: priceChartData }],*/}
            {/*    }}/>*/}
            {/*</div>*/}
            <div className={s.priceChartStatsContainer}>
                <div className={s.priceChartStat}>
                    <span className={s.priceChartTextLabel}>Quantity Sold</span>
                    <span style={{ fontWeight: 500 }}>{store.resaleData.sales}</span>
                </div>
                <div className={s.priceChartStat}>
                    <span className={s.priceChartTextLabel}>Original Price</span>
                    <div className={`${s.priceContainer} flex`}>
                        <span className={`icon-robux ${s.priceIcon}`}/>
                        <span className={s.priceLabel}
                              style={{ color: "var(--robux-color)" }}>{formatNum(store.details.price)}</span>
                    </div>
                </div>
                <div className={s.priceChartStat}>
                    <span className={s.priceChartTextLabel}>Average Price</span>
                    <div className={`${s.priceContainer} flex`}>
                        <span className={`icon-robux ${s.priceIcon}`}/>
                        <span className={s.priceLabel}
                              style={{ color: "var(--robux-color)" }}>{formatNum(store.resaleData.recentAveragePrice)}</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
}

export default PriceChart;
