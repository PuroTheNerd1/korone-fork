import React, { useRef } from "react";
import { createUseStyles } from "react-jss";
import getFlag from "../../../lib/getFlag";
import {
    getAvPageStyle,
    getCatalogPageStyle, getCatalogStyle,
    getTheme, getThemeColor, getThemeFont,
    getThemeCustomColor, setThemeCustomColor,
    setAvPageStyle,
    setCatalogPageStyle, setCatalogStyle,
    setTheme, setThemeColor, setThemeFont, setThemeForumHeader, setThemeRibbon, themeType
} from "../../../services/theme";
import useCardStyles from "../../userProfile/styles/card";
import MyAccountStore from "../stores/myAccountStore"
import useFormStyles from "../styles/forms";
import Subtitle from "./subtitle";
import { setAvPageStyleReq } from "../../../services/accountSettings";
import { ChangeVarsForTheme, ChangeVarsForThemeColor, ChangeVarsForThemeFont } from "../../../pages/_app";

const Customization = props => {
    const store = MyAccountStore.useContainer();
    const colorInputRef = useRef(null);

    const isDebug = false;
    const year = new Date().getFullYear();
    const christmasStart = new Date(year, 10, 28);
    const christmasEnd = new Date(year, 12, 31);
    const halloweenStart = new Date(year, 9, 1);
    const halloweenEnd = new Date(year, 10, 5);

    const cardStyles = useCardStyles();
    const s = useFormStyles();
    return <div className='row'>
        {getFlag('settingsPageThemeSelectorEnabled', false) &&
            <div className='col-12 mt-2'>
                <Subtitle>Customize Your Korone</Subtitle>
                <div className={cardStyles.card + ' p-3'}>
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Website Theme'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0'>
                            <select className={'form-control ' + s.select} value={store.theme.theme} onChange={(ev) => {
                                setTheme(ev.currentTarget.value);
                                store.setTheme({
                                    ...store.theme,
                                    theme: ev.target.value,
                                });
                                window.location.reload();
                            }}>
                                <option value='light'>Light Theme</option>
                                <option value='obc2019'>OBC Theme</option>
                                <option value='dark'>Dark Theme (BETA)</option>
                            </select>
                        </div>
                    </div>
                    {
                        getTheme() === themeType.dark || getTheme() === themeType.obc2019
                        ?
                        <div className='flex mt-1'>
                            <div className='col pe-0'>
                                <input className={'form-control ' + s.select + ' ' + s.disabled} value='Apply Theme to Ribbonbar'
                                       readOnly={true}
                                       type='text'></input>
                            </div>
                            <div className='col ps-0 pe-0'>
                                <select className={'form-control ' + s.select} value={store.theme.themeRibbon} onChange={(ev) => {
                                    setThemeRibbon(ev.currentTarget.value);
                                    store.setTheme({
                                        ...store.theme,
                                        themeRibbon: ev.target.value,
                                    });
                                    window.location.reload();
                                }}>
                                    <option value='true'>Yes</option>
                                    <option value='false'>No</option>
                                </select>
                            </div>
                        </div>
                        :
                        null
                    }
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Website Color'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0'>
                            <select className={'form-control ' + s.select} value={store.theme.color} onChange={(ev) => {
                                setThemeColor(ev.currentTarget.value);
                                ChangeVarsForThemeColor(ev.currentTarget.value);
                                store.setTheme({
                                    ...store.theme,
                                    color: ev.target.value,
                                });
                            }}>
                                {
                                    isDebug || year >= christmasStart && year <= christmasEnd ? <option value='cane'>Cane</option> : null
                                }
                                {
                                    isDebug || year >= halloweenStart && year <= halloweenEnd ? <option value='spice'>Spice</option> : null
                                }
                                <option value='custom'>Custom</option>
                                <option value='coffee'>Coffee</option>
                                <option value='bliss'>Bliss</option>
                                <option value='cobalt'>Cobalt</option>
                                <option value='sunlit'>Sunlit</option>
                                <option value='royalty'>Royalty</option>
                                <option value='nobility'>Nobility (zyth's color :3)</option>
                                <option value='harmony'>Harmony</option>
                                <option value='witness'>Witness</option>
                                <option value='whisper'>Velvet Whisper</option>
                            </select>
                        </div>
                    </div>
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Custom Website Color'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0' style={{position: 'relative'}}>
                            <div
                                onClick={() => colorInputRef.current.click()}
                                className={'form-control ' + s.select}
                                style={{display: 'flex', alignItems: 'center', cursor: 'pointer', height: '100%'}}
                            >
                                <div style={{width: 20, height: 20, background: store.theme.customColor || '#8A5149', borderRadius: 3, marginRight: 8, border: '1px solid #aaa', flexShrink: 0}}></div>
                                <span style={{fontFamily: 'monospace', fontSize: 13}}>{store.theme.customColor || '#8A5149'}</span>
                            </div>
                            <input
                                ref={colorInputRef}
                                type='color'
                                value={store.theme.customColor || '#8A5149'}
                                onChange={(e) => {
                                    const hex = e.target.value;
                                    setThemeCustomColor(hex);
                                    store.setTheme({...store.theme, customColor: hex});
                                    if (store.theme.color === 'custom') {
                                        ChangeVarsForThemeColor('custom');
                                    }
                                }}
                                style={{position: 'absolute', opacity: 0, width: 0, height: 0, pointerEvents: 'none'}}
                            />
                        </div>
                    </div>
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Website Font'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0'>
                            <select className={'form-control ' + s.select} value={store.theme.font} onChange={(ev) => {
                                setThemeFont(ev.currentTarget.value);
                                ChangeVarsForThemeFont(ev.currentTarget.value);
                                store.setTheme({
                                    ...store.theme,
                                    font: ev.target.value,
                                });
                            }}>
                                <option value='gotham'>Gotham SSm</option>
                                <option value='ssp'>Source Sans Pro</option>
                            </select>
                        </div>
                    </div>
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Stylize Forum Headers'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0'>
                            <select className={'form-control ' + s.select} value={store.theme.stylizeForumHeader} onChange={(ev) => {
                                setThemeForumHeader(ev.currentTarget.value);
                                store.setTheme({
                                    ...store.theme,
                                    stylizeForumHeader: ev.target.value,
                                });
                            }}>
                                <option value='true'>Yes</option>
                                <option value='false'>No</option>
                            </select>
                        </div>
                    </div>
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Catalog Style'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0'>
                            <select className={'form-control ' + s.select} value={getCatalogStyle()}
                                    onChange={ev => {
                                        setCatalogStyle(ev.currentTarget.value);
                                        window.location.replace(window.location.pathname + '?t=' + new Date().getTime());
                                    }}>
                                <option value="Modern">Modern (2017+)</option>
                                <option value="Legacy">Legacy (2012-2017)</option>
                            </select>
                        </div>
                    </div>
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Avatar Page Style'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0'>
                            <select className={'form-control ' + s.select} value={getAvPageStyle()} onChange={(ev) => {
                                setAvPageStyle(ev.currentTarget.value);
                                setAvPageStyleReq({ newAvatarPageStyle: ev.currentTarget.value }).then();
                                window.location.reload();
                            }}>
                                <option value="Modern">Modern (2017+)</option>
                                <option value="Legacy">Legacy (2013-2017)</option>
                            </select>
                        </div>
                    </div>
                    <div className='flex mt-1'>
                        <div className='col pe-0'>
                            <input className={'form-control ' + s.select + ' ' + s.disabled} value='Asset Details Page Style'
                                   readOnly={true}
                                   type='text'></input>
                        </div>
                        <div className='col ps-0 pe-0'>
                            <select className={'form-control ' + s.select} value={getCatalogPageStyle()}
                                    onChange={ev => {
                                        setCatalogPageStyle(ev.currentTarget.value);
                                        window.location.reload();
                                    }}>
                                <option value="Modern">Modern (2017+)</option>
                                <option value="Legacy">Legacy (2012-2017)</option>
                            </select>
                        </div>
                    </div>
                </div>
            </div>
        }
    </div>
}

export default Customization;
