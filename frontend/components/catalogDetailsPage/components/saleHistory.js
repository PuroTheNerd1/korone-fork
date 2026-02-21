import React, { useEffect, useState } from "react";
import { createUseStyles } from "react-jss";
import { getResaleData } from "../../../services/economy";
import CatalogDetailsPage from "../stores/catalogDetailsPage";
import dayjs from "dayjs";
import Robux from "./robux";

const useStyles = createUseStyles({
  wrapper: {
    background: '#f2f2f2',
    padding: '12px 16px 10px',
    marginTop: '55px',
  },
  title: {
    fontSize: '18px',
    fontWeight: 400,
    marginBottom: '6px',
    color: '#333',
  },
  headerRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '6px',
  },
  legend: {
    display: 'flex',
    gap: '12px',
    alignItems: 'center',
    fontSize: '12px',
    color: '#555',
  },
  legendItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '5px',
  },
  legendLine: {
    width: '22px',
    height: '3px',
    display: 'inline-block',
    borderRadius: '2px',
  },
  select: {
    padding: '2px 6px',
    fontSize: '13px',
    border: '1px solid #ccc',
    borderRadius: '3px',
    background: 'white',
    cursor: 'pointer',
  },
  noData: {
    textAlign: 'center',
    color: '#888',
    padding: '20px 0',
    fontSize: '13px',
  },
  statsRow: {
    display: 'flex',
    borderTop: '1px solid #ddd',
    marginTop: '10px',
    paddingTop: '8px',
  },
  statItem: {
    flex: 1,
    textAlign: 'center',
    '& + &': {
      borderLeft: '1px solid #ddd',
    },
  },
  statLabel: {
    color: '#666',
    fontSize: '12px',
    marginBottom: '3px',
  },
  statValue: {
    fontWeight: 700,
    fontSize: '14px',
    color: '#333',
    marginBottom: 0,
  },
});

const SaleHistory = props => {
  const store = CatalogDetailsPage.useContainer();
  const [rap, setRap] = useState(null);
  const [rapChart, setRapChart] = useState(null);
  const [volumeChart, setVolumeChart] = useState(null);
  const [dataLoaded, setDataLoaded] = useState(false);
  const s = useStyles();

  useEffect(() => {
    if (!store.details) return;
    setDataLoaded(false);
    setRap(null);
    setRapChart(null);
    setVolumeChart(null);
    getResaleData({ assetId: store.details.id }).then(resaleData => {
      setRap(resaleData.recentAveragePrice);
      setRapChart(resaleData.priceDataPoints);
      setVolumeChart(resaleData.volumeDataPoints);
      setDataLoaded(true);
      if (store.saleCount === 0) {
        store.setSaleCount(resaleData.sales);
      }
    });
  }, [store.details]);

  useEffect(() => {
    if (!rapChart || !volumeChart || rapChart.length === 0) {
      return;
    }
    const rapData = rapChart.map(v => [dayjs(v.date).unix() * 1000, v.value]);
    const volumeData = volumeChart.map(v => [dayjs(v.date).unix() * 1000, v.value]);
    const render = () => {
      // @ts-ignore
      requestAnimationFrame(() => window.RobloxItemChartLibrary.loadChart(rapData, volumeData));
    };
    // @ts-ignore
    if (!window.RobloxItemChartLibrary) {
      const saleChartScript = document.createElement('script');
      saleChartScript.setAttribute('src', '/js/itemSaleChart.js?refresh=2');
      saleChartScript.onload = render;
      document.body.appendChild(saleChartScript);
    } else {
      render();
    }
  }, [rapChart, volumeChart]);

  if (!dataLoaded) {
    return null;
  }

  const originalPrice = store.details && store.details.price;
  const hasChart = rapChart && rapChart.length > 0;

  return (
    <div className={s.wrapper}>
      <p className={s.title}>Price Chart</p>
      <div className={s.headerRow}>
        <div className={s.legend}>
          <span className={s.legendItem}>
            <span className={s.legendLine} style={{ background: '#008000' }}></span>
            Recent Average Price
          </span>
          <span className={s.legendItem}>
            <span className={s.legendLine} style={{ background: '#A4A4C8' }}></span>
            Volume
          </span>
        </div>
        <select id="daysSelect" className={s.select} defaultValue="180">
          <option value="30">30 Days</option>
          <option value="90">90 Days</option>
          <option value="180">180 Days</option>
        </select>
      </div>
      {hasChart ? (
        <div id='placeholder' style={{ width: '100%', height: '220px' }}></div>
      ) : (
        <p className={s.noData}>No price data available.</p>
      )}
      <div className={s.statsRow}>
        <div className={s.statItem}>
          <p className={s.statLabel}>Quantity Sold</p>
          <p className={s.statValue}>{(store.saleCount || 0).toLocaleString()}</p>
        </div>
        <div className={s.statItem}>
          <p className={s.statLabel}>Original Price</p>
          {originalPrice != null
            ? <Robux inline>{originalPrice.toLocaleString()}</Robux>
            : <p className={s.statValue}>-</p>
          }
        </div>
        <div className={s.statItem}>
          <p className={s.statLabel}>Average Price</p>
          {rap != null
            ? <Robux inline>{rap.toLocaleString()}</Robux>
            : <p className={s.statValue}>-</p>
          }
        </div>
      </div>
    </div>
  );
}

export default SaleHistory;
