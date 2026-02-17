const themeType = {
    dark: 'dark',
    obc2019: 'obc2019',
    bliss: 'bliss',
    light: 'light',
    default: 'light',
}

const themeColor = {
    coffee: 'coffee',
    bliss: 'bliss',
    cobalt: 'cobalt',
    sunlit: 'sunlit',
    royalty: 'royalty',
    nobility: 'nobility',
    harmony: 'harmony',
    witness: 'witness',
    whisper: 'whisper',
    cane: 'cane',
    spice: 'spice',
    custom: 'custom',
}

const themeFont = {
    gotham: 'gotham',
    ssp: 'ssp',
}

const avPageStyleType = {
    Modern: 'Modern',
    Legacy: 'Legacy',
}

const catalogPageStyle = {
    Modern: 'Modern',
    Legacy: 'Legacy',
}

const isLocalStorageAvailable = (() => {
    // @ts-ignore
    if (!process.browser) return false;
    if (typeof window === 'undefined' || !window.localStorage || !window.localStorage.getItem || !window.localStorage.setItem) return false;
    
    return true;
})()

const getTheme = () => {
    if (!isLocalStorageAvailable) return themeType.default;
    
    let value = localStorage.getItem('rbx_theme_v1');
    // validate
    if (typeof value !== 'string' || !Object.getOwnPropertyNames(themeType).includes(value)) return themeType.default;
    return themeType[value];
}

const setTheme = (themeString) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_theme_v1', themeString)
}

const getThemeRibbon = () => {
    if (!isLocalStorageAvailable) return 'false';
    
    let value = localStorage.getItem('rbx_theme_ribbon_v1');
    // validate
    if (typeof value !== 'string' || !/^(true|false)$/i.test(value)) return 'false';
    return value;
}

const setThemeRibbon = (themeString) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_theme_ribbon_v1', themeString)
}

const getThemeColor = () => {
    if (!isLocalStorageAvailable) return themeColor.coffee;
    
    let value = localStorage.getItem('rbx_theme_color_v1');
    // validate
    if (typeof value !== 'string' || !Object.getOwnPropertyNames(themeColor).includes(value)) return themeColor.coffee;
    return themeColor[value];
}

const setThemeColor = (themeString) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_theme_color_v1', themeString);
}

const getThemeFont = () => {
    if (!isLocalStorageAvailable) return themeFont.gotham;
    
    let value = localStorage.getItem('rbx_theme_font_v1');
    // validate
    if (typeof value !== 'string' || !Object.getOwnPropertyNames(themeFont).includes(value)) return themeFont.gotham;
    return themeFont[value];
}

const setThemeFont = (themeString) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_theme_font_v1', themeString)
}

const getThemeForumHeader = () => {
    if (!isLocalStorageAvailable) return 'false';
    
    let value = localStorage.getItem('rbx_theme_forum_header_v1');
    // validate
    if (typeof value !== 'string' || !/^(true|false)$/i.test(value)) return 'false';
    return value;
}

const setThemeForumHeader = (bool) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_theme_forum_header_v1', bool)
}

const getAvPageStyle = () => {
    if (!isLocalStorageAvailable) return avPageStyleType.default;
    
    let value = localStorage.getItem('rbx_av_page_style_v1');
    // validate
    if (typeof value !== 'string' || !Object.getOwnPropertyNames(avPageStyleType).includes(value)) return avPageStyleType.default;
    return avPageStyleType[value];
}

const setAvPageStyle = (themeString) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_av_page_style_v1', themeString)
}

const getCatalogPageStyle = () => {
    if (!isLocalStorageAvailable) return catalogPageStyle["Modern"];

    let value = localStorage.getItem('rbx_cat_page_style_v1');
    // validate
    if (typeof value !== 'string' || !Object.getOwnPropertyNames(catalogPageStyle).includes(value)) return catalogPageStyle["Modern"];
    return catalogPageStyle[value];
}

const setCatalogPageStyle = (themeString) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_cat_page_style_v1', themeString);
}

const getCatalogStyle = () => {
    if (!isLocalStorageAvailable) return catalogPageStyle["Modern"];

    let value = localStorage.getItem('rbx_cat_style_v1');
    // validate
    if (typeof value !== 'string' || !Object.getOwnPropertyNames(catalogPageStyle).includes(value)) return catalogPageStyle["Modern"];
    return catalogPageStyle[value];
}

const setCatalogStyle = (themeString) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_cat_style_v1', themeString);
}

const getThemeCustomColor = () => {
    if (!isLocalStorageAvailable) return null;
    let value = localStorage.getItem('rbx_theme_custom_color_v1');
    if (typeof value !== 'string' || !/^#[0-9A-Fa-f]{6}$/.test(value)) return null;
    return value;
}

const setThemeCustomColor = (hex) => {
    if (!isLocalStorageAvailable) return;
    localStorage.setItem('rbx_theme_custom_color_v1', hex);
}

export {
    getTheme,
    setTheme,
    
    getThemeFont,
    setThemeFont,
    
    getThemeColor,
    setThemeColor,
    
    getThemeForumHeader,
    setThemeForumHeader,
    
    getThemeRibbon,
    setThemeRibbon,
    
    getAvPageStyle,
    setAvPageStyle,
    
    getCatalogPageStyle,
    setCatalogPageStyle,

    getCatalogStyle,
    setCatalogStyle,

    getThemeCustomColor,
    setThemeCustomColor,

    themeType,
    themeColor,
    themeFont,
    avPageStyleType,
    catalogPageStyle,
}