import { useState } from "react";
import { createUseStyles } from "react-jss";
import useCardStyles from "../userProfile/styles/card";
import Link from "../link";

const useStyles = createUseStyles({
  wrapper: {
    //marginTop: '-20px',
    cursor: 'pointer',
    userSelect: 'none',
    lineHeight: '100%',
    //display: 'flex',
    '&:hover': {
    },
  },
  dropdown: {
    width: '125px',
    background: 'white',
    position: 'absolute',
    right: '-20px',
    borderRadius: '2px',
  },
  dropdownEntry: {
    width: '100%',
    '&:hover': {
      background: '#e3e3e3',
    },
  },
  dropdownText: {
    marginBottom: 0,
    fontSize: '16px',
    padding: '8px 10px',
    color: 'rgb(33, 37, 41)',
  },
  dropdownDots: {
    letterSpacing: '3px',
    fontWeight: 100,
  },

  dropdownButton: {
    background: 'transparent',
    border: '0 transparent',
    padding: 0,
    color: '#191919',
    userSelect: 'none',
    cursor: 'pointer',
    fontWeight: '500',
    height: 'auto',
    textAlign: 'center',
    whiteSpace: 'nowrap',
    verticalAlign: 'middle',
    margin: 0,
    lineHeight: '18px',
    width: '100%',
  },
  dropdownIcon: {
    backgroundPosition: '0 -616px',
    backgroundSize: '200% auto',
    width: '28px',
    height: '28px',
    margin: 0,
    padding: 0,
    float: 'right',
    display: 'inline-block',
    backgroundImage: 'url(/img/generic.svg)',
    backgroundRepeat: 'no-repeat',
    verticalAlign: 'middle',
    '&:hover': {
      backgroundPosition: '-28px -616px',
    }
  },

  dropdownNew: {
    color: '#191919',
    width: 'auto',
    right: '5px',
    left: 'auto',
    backgroundColor: '#fff',
    boxShadow: '0 -5px 20px rgba(25, 25, 25, 0.15)!important',
    maxHeight: '266px',
    borderRadius: '4px',
    backgroundClip: 'padding-box',
    float: 'left',
    fontSize: '16px',
    margin: 0,
    padding: 0,
    position: 'absolute',
    minWidth: '105px',
    overflowX: 'hidden',
    overflowY: 'auto',
    listStyle: 'none',
    textAlign: 'left',
    zIndex: '1123'
  },

  dropdownItem: {
    color: '#191919',
    padding: 0,
    margin: 0,
    whiteSpace: 'nowrap',
    width: '100%',
    listStyle: 'none',
    display: 'list-item',
    '&:hover': {
      backgroundColor: '#f2f2f2 !important',
      boxShadow: 'inset 4px 0 0 0 #00a2ff',
      color: '#191919'
    }
  },
  dropdownItemLink: {
    padding: '10px 12px',
    display: 'block',
    clear: 'both',
    lineHeight: '1.428571429',
    whiteSpace: 'nowrap',
    border: 'none',
    background: 'transparent',
    width: '100%',
    textAlign: 'left',
    textDecoration: 'none',
    cursor: 'pointer',
    fontSize: '16px',
    userSelect: 'none',
    color: '#191919',
    '&:hover': {
      color: '#191919'
    }
  },
  centered: {
    right: '20px'
  }
});

/**
 * Dropdown
 * @param {{onlyDropdown?: boolean; options: {url?: string; name: string; onClick?: (e: any) => void}[], centered?: boolean, wrapperClass?: string; dropdownClass?: string;}} props 
 * @returns 
 */
const Dropdown2016 = props => {
  const [isOpen, setIsOpen] = useState(false);
  const cardStyles = useCardStyles();
  const s = useStyles();

  return <div className={`${s.wrapper} ${props?.wrapperClass}`}>
    {/*<p className={'mb-0 ' + s.dropdownDots} onClick={() => {
      setIsOpen(!isOpen);
    }}>...</p>*/}

    {!props.onlyDropdown && <button className={s.dropdownButton}>
      <span className={s.dropdownIcon} onClick={() => {
        setIsOpen(!isOpen);
      }}></span>
    </button>}
    {
      isOpen || props.onlyDropdown ? <ul className={`${s.dropdownNew} ${cardStyles.card} ${props.centered ? s.centered : ''} ${props.dropdownClass}`}>
        {
          props.options.map(v => {
            /*return <a href={v.url || '#'} onClick={v.onClick}>
              <div className={s.dropdownEntry} key={v.name}>
                <p className={s.dropdownText}>{v.name}</p>
              </div>
            </a>*/
            return <li className={s.dropdownItem} key={v.name}>
              <Link href={v.url || '#'} onClick={v.onClick}>
                <a className={s.dropdownItemLink} onClick={v.onClick} href={v.url || '#'}>{v.name}</a>
              </Link>
            </li>
          })
        }
      </ul> : null
    }
  </div>
}

export default Dropdown2016;