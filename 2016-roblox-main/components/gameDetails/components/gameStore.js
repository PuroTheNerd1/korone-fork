import { createUseStyles } from "react-jss";
import GameDetailsStore from "../stores/gameDetailsStore";
import {useEffect, useState} from "react";

const useStyles = createUseStyles({
  tabPane: {
    backgroundColor: '#fff',
    padding: '12px',
    display: 'flex',
    flexDirection: 'column',
  },
  gamePassContainer:{
    overflow: 'hidden',
    margin: '0 0 6px',
    padding: '0'
  },
  containerHeader:{
    fontSize: '16px',
    fontWeight: '400',
    lineHeight: '1.4em',
    display: 'flex',
  },
  containerHeaderText:{
    fontSize: '20px',
    fontWeight: '700',
    float: 'left',
    margin: 0,
    lineHeight: '1.4em',
    paddingBottom: '5px'
  },
  noPasses:{
    color: '#757575',
    background: 'transparent !important',
    padding: '15px',
    textAlign: 'center',
    lineHeight: '1.5em',
    margin: 0,
    wordWrap: 'break-word',
    hyphens: 'none',
  },
  passesContainer:{
    display: 'flex',
    flexWrap: 'wrap',
    listStyle: 'none',
    margin: 0,
    padding: 0,
  },
  listItem:{
    width: '16.6666666667%',
    marginBottom: '12px',
    float: 'left',
    display: 'list-item',
  },
  passCard:{
    border: '1px solid #E3E3E3',
    backgroundColor: '#fff',
    position: 'relative',
    borderRadius: '3px',
    margin: '0 5% 0 0',
    maxWidth: '150px',
    padding: 0,
  },
  passPicture:{
    display: 'block',
    textAlign: 'center',
    '& img':{
      width: '150px',
      height: '150px',
      borderRadius: '3px',
      border: 0,
      verticalAlign: 'middle',
    }
  },
  passCaption:{
    borderTop: '1px solid #b8b8b8',
    padding: '0 6px 6px',
  },
  passName:{
    fontWeight: '500',
    margin: '3px auto',
    fontSize: '16px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  passPriceContainer:{
    lineHeight: '1em',
  },
  passFooter:{
    
  },
})


const GameStats = props => {
  const store = GameDetailsStore.useContainer();
  const hasPasses = false;
  const s = useStyles();
  //if (!store.placeDetails || !store.universeDetails) return null;
  return <div className={' ' + s.tabPane}>
    <div className={s.gamePassContainer}>
      <div className={s.containerHeader}>
        <h3 className={s.containerHeaderText}>Passes for this game</h3>
      </div>
      {hasPasses ? 
      <ul className={s.passesContainer}>
        j
      </ul> 
      : 
      <p className={s.noPasses}>No passes available.</p>}
    </div>
  </div>
}

export default GameStats;