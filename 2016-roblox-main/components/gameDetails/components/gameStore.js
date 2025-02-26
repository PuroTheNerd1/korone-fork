import { createUseStyles } from "react-jss";
import GameDetailsStore from "../stores/gameDetailsStore";
import {useEffect, useState} from "react";
import {getUniverseGamePasses} from "../../../services/games";
import ItemImage from "../../itemImage";
import ActionButton from "../../actionButton";
import useButtonStyles from "../../../styles/buttonStyles";
import Link from "../../link";
import {getItemUrl} from "../../../services/catalog";
import ActionLink from "../../actionLink";
import Robux2 from "../../robux2";

const useStyles = createUseStyles({
  tabPane: {
    //backgroundColor: 'var(--white-color)',
    //padding: '12px',
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
    fontWeight: '700',
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
    color: 'var(--text-color-tertiary)',
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
    //padding: 6, // accounts for box shadowing // disabled due to it looking weirder with this on than off (both weird)
  },
  listItem:{
    width: '16.6666666667%',
    marginBottom: '12px',
    float: 'left',
    display: 'list-item',
  },
  passCard:{
    border: '1px solid var(--background-color)',
    backgroundColor: 'var(--white-color)',
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
    borderTop: '1px solid var(--text-color-secondary)',
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
  passFooter: {
  },
  
  // new code because i dont trust the code above lol
  
  gPassWrapper: {
    width: '16.66667%',
    marginBottom: 12
  },
  gPassContainer: {
    display: 'flex',
    flexDirection: 'column',
    maxWidth: '150px',
    borderRadius: 3,
    margin: '0 5% 0 0',
    padding: 0,
    overflow: 'hidden'
  },
  gPassImg: {
    width: 150,
    height: 150,
    margin: 0,
    padding: 0
  },
  gPassDetails: {
    display: 'flex',
    flexDirection: 'column',
    borderTop: '1px solid #b8b8b8',
    padding: '0 6px 6px'
  },
  gPassName: {
    fontWeight: 500,
    margin: '3px auto',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    width: '100%',
  },
  gPassPriceContainer: {},
  gPassBuyContainer: {
    marginTop: 6,
  },
  gPassBuyButton: {
    padding: 9,
    fontSize: 18,
    fontWeight: 500,
    lineHeight: '100%',
    '&:hover': {
      backgroundColor: '#3FC679',
      boxShadow: '0 1px 3px rgba(150,150,150,0.74)',
      borderColor: '#3FC679!important',
      color: '#fff!important',
      cursor: 'pointer',
    }
  },
})

/**
 *
 * @param {{ id: number; name: string; price: number; }} props
 * @constructor
 */
const GamePassEntry = ({ id, name, price }) => {
  const s = useStyles();
  const buttonStyles = useButtonStyles();
  
  return <div className={s.gPassWrapper}>
    <div className={`section-content hoverShadow ${s.gPassContainer}`}>
      <Link href={getItemUrl({assetId: id, name})}>
        <ItemImage className={s.gPassImg} id={id} name={name}/>
      </Link>
      <div className={s.gPassDetails}>
        <span className={s.gPassName}>{name}</span>
        <span className={s.gPassPriceContainer}>
        <Robux2>{price}</Robux2>
      </span>
        <div className={s.gPassBuyContainer}>
            <ActionLink className={s.gPassBuyButton} label='Buy' buttonStyle={buttonStyles.newCancelButton}
                        href={getItemUrl({assetId: id, name})}/>
        </div>
      </div>
    </div>
  </div>
}

const GameStore = props => {
  const s = useStyles();
  const store = GameDetailsStore.useContainer();
  if (!store.placeDetails || !store.universeDetails) return null;
  // 0 == loading, 1 == no passes, 2 == failed to load, array is success
  const [passes, setPasses] = useState(0);
  
  useEffect(() => {
    setPasses(0);
    getUniverseGamePasses({universeId: store.universeDetails.id}).then(d => {
      try { // accounts for d being null if for whatever rerason it is
        if (d.length === 0) {
          setPasses(1);
          return;
        }
        setPasses(d);
      } catch (e) {
        setPasses(2);
      }
    })
  }, [store.universeDetails])
  
  return <div className={' ' + s.tabPane}>
    <div className={s.gamePassContainer}>
      <div className={s.containerHeader}>
        <h3 className={s.containerHeaderText}>Passes for this game</h3>
      </div>
      {Array.isArray(passes) ? <ul className={s.passesContainer}>
        {passes.map(pass => <GamePassEntry key={pass.id} id={pass.id} name={pass.name} price={pass.price} />)}
      </ul> : passes === 0 ? <p className={s.noPasses}>Loading gamepasses...</p> : passes === 1 ?
          <p className={s.noPasses}>No passes available.</p> : <p className={s.noPasses}>An error occurred while loading gamepasses.</p>}
    </div>
  </div>
}

export default GameStore;