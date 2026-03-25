import '../styles/globals.css';
import '../styles/helpers/textHelpers.css';
import 'bootstrap/dist/css/bootstrap.min.css';
// Roblox CSS
import '../styles/roblox/icons.css';
import Navbar from '../components/navbar';
import React, {useEffect} from 'react';
import Head from 'next/head';
import Footer from '../components/footer';
import NextNProgress from "nextjs-progressbar";
import LoginModalStore from '../stores/loginModal';
import AuthenticationStore from '../stores/authentication';
import NavigationStore from '../stores/navigation';
import { getTheme, getThemeColor, getThemeFont, getThemeCustomColor, themeColor, themeFont, themeType } from '../services/theme';
import MainWrapper from '../components/mainWrapper';
import GlobalAlert from '../components/globalAlert';
import ThumbnailStore from "../stores/thumbnailStore";
import getFlag from "../lib/getFlag";
import Chat from "../components/chat";
import ChatStore from "../components/chat/chatStore";
import FeedbackStore from "../stores/feedback";
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime.js'

if (typeof window !== 'undefined') {
    console.log(String.raw`
      _______      _________      _____       ______     _
     / _____ \    |____ ____|    / ___ \     | ____ \   | |
    / /     \_\       | |       / /   \ \    | |   \ \  | |
    | |               | |      / /     \ \   | |   | |  | |
    \ \______         | |      | |     | |   | |___/ /  | |
     \______ \        | |      | |     | |   |  ____/   | |
            \ \       | |      | |     | |   | |        | |
     _      | |       | |      \ \     / /   | |        |_|
    \ \_____/ /       | |       \ \___/ /    | |         _
     \_______/        |_|        \_____/     |_|        |_|

     Keep your account safe! Do not paste any text here.`);
}

function RobloxApp({Component, pageProps}) {
    // set theme:
    // jss globals apparently don't support parameters/props, so the only way to do a dynamic global style is to either append a <style> element, use setAttribute(), or append a css file.
    // @ts-ignore
    const isChristmas = false
    useEffect(() => {
        dayjs.extend(relativeTime);
        const el = typeof window !== 'undefined' && document.getElementsByTagName('body');
        if (el && el.length) {
            const theme = getTheme();
            const themeColor = getThemeColor();
            const themeFont = getThemeFont();
            const divBackground =
                theme === themeType.dark || theme === themeType.obc2019
                ?
                'url(/img/Unofficial/obc_theme_2016_bg.png) repeat-x #222224'
                :
                document.getElementById('theme-2016-enabled')
                ?
                '#e3e3e3'
                :
                '#fff'
            ;
            el[0].setAttribute('style', 'background: ' + divBackground);
            ChangeVarsForTheme(theme);
            ChangeVarsForThemeColor(themeColor);
            ChangeVarsForThemeFont(themeFont);
        }
    }, [pageProps]);
    
    return <div style={pageProps.disableWebsiteTheming ? {minHeight: '100vh'} : null}>
        <Head>
            <link rel="preconnect" href="https://fonts.googleapis.com"/>
            <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin={''}/>
            <title>{pageProps.title || 'Korone'}</title>
            <link rel='icon' type="image/vnd.microsoft.icon" href='/favicon.ico'/>
            <meta name='viewport' content='width=device-width, initial-scale=1'/>
            <script async src="https://influx.korone.one/js/pa-5rtt1_DLHJRTjtc_ir3nW.js"></script>
        </Head>
        <AuthenticationStore.Provider>
            {pageProps.disableWebsiteTheming ? null : <>
                <LoginModalStore.Provider>
                    <NavigationStore.Provider>
                        <Navbar/>
                    </NavigationStore.Provider>
                </LoginModalStore.Provider>
                <GlobalAlert/>
            </>}
            <FeedbackStore.Provider>
                <MainWrapper mainFlex={pageProps.disableWebsiteTheming}>
                    {getFlag('clientSideRenderingEnabled', false) ?
                        <NextNProgress options={{showSpinner: true}} color='var(--primary-color)' height={4}/> : null}
                    <ThumbnailStore.Provider>
                        <ChatStore.Provider>
                            <Component {...pageProps} />
                            <Chat/>
                        </ChatStore.Provider>
                    </ThumbnailStore.Provider>
                </MainWrapper>
            </FeedbackStore.Provider>
            {pageProps.disableWebsiteTheming ? null : <Footer/>}
        </AuthenticationStore.Provider>
    </div>
}

function ChangeVarsForTheme(theme) {
    switch (theme) {
        case themeType.dark:
            document.documentElement.style.setProperty('--text-color-primary', '#fff');
            document.documentElement.style.setProperty('--text-color-secondary', '#5a5a5a');
            document.documentElement.style.setProperty('--text-color-tertiary', '#999');
            document.documentElement.style.setProperty('--white-color', '#191919');
            document.documentElement.style.setProperty('--white-color-hover', '#212121');
            //document.documentElement.style.setProperty('--background-color', '#393939');
            document.documentElement.style.setProperty('--background-color', 'transparent');
            document.documentElement.style.setProperty('--text-color-quinary', '#b8b8b8');
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.style.setProperty('--text-color-quinary', '#5b5b5b');
            break;
        default:
            break;
    }
}

function adjustHexBrightness(hex, factor) {
    const r = Math.min(255, Math.round(parseInt(hex.slice(1, 3), 16) * factor));
    const g = Math.min(255, Math.round(parseInt(hex.slice(3, 5), 16) * factor));
    const b = Math.min(255, Math.round(parseInt(hex.slice(5, 7), 16) * factor));
    return '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0')).join('');
}

function ChangeVarsForThemeColor(theme) {
    switch (theme) {
        case themeColor.coffee:
            document.documentElement.style.setProperty('--primary-color', '#8A5149');
            document.documentElement.style.setProperty('--primary-color-2', '#915A4D');
            document.documentElement.style.setProperty('--primary-color-hover', '#9C6A5E');
            document.documentElement.style.setProperty('--secondary-color', '#653E35');
            break;
        case themeColor.bliss:
            document.documentElement.style.setProperty('--primary-color', 'var(--blue-color)');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--blue-color-2)');
            document.documentElement.style.setProperty('--primary-color-hover', 'var(--blue-color-hover)');
            document.documentElement.style.setProperty('--secondary-color', '#0074bd');
            break;
        case themeColor.cobalt:
            document.documentElement.style.setProperty('--primary-color', '#313233');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#454647');
            document.documentElement.style.setProperty('--secondary-color', '#272828');
            break;
        case themeColor.sunlit:
            document.documentElement.style.setProperty('--primary-color', '#ff911c');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#FFA749');
            document.documentElement.style.setProperty('--secondary-color', '#CC7416');
            break;
        case themeColor.royalty:
            document.documentElement.style.setProperty('--primary-color', '#5100A5');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#6600CF');
            document.documentElement.style.setProperty('--secondary-color', '#400084');
            break;
        case themeColor.nobility:
            document.documentElement.style.setProperty('--primary-color', '#6600CF');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#8432D8');
            document.documentElement.style.setProperty('--secondary-color', '#5100A5');
            break;
        case themeColor.harmony:
            document.documentElement.style.setProperty('--primary-color', '#5FA554');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#6FAE65');
            document.documentElement.style.setProperty('--secondary-color', '#42733A');
            break;
        case themeColor.witness:
            document.documentElement.style.setProperty('--primary-color', '#74CD81');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#8FD79A');
            document.documentElement.style.setProperty('--secondary-color', '#5CA467');
            break;
        case themeColor.whisper:
            document.documentElement.style.setProperty('--primary-color', '#EEA1CD');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#f1b3d7');
            document.documentElement.style.setProperty('--secondary-color', '#F4B8DA');
            break;
        case themeColor.cane: // christmas theme!
            document.documentElement.style.setProperty('--primary-color', '#ae003e');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#d20057');
            document.documentElement.style.setProperty('--secondary-color', '#960033');
            break;
        case themeColor.spice: // halloween theme!
            document.documentElement.style.setProperty('--primary-color', '#ae003e');
            document.documentElement.style.setProperty('--primary-color-2', 'var(--primary-color)');
            document.documentElement.style.setProperty('--primary-color-hover', '#d20057');
            document.documentElement.style.setProperty('--secondary-color', '#960033');
            break;
        case themeColor.custom: {
            const customHex = getThemeCustomColor();
            if (customHex) {
                document.documentElement.style.setProperty('--primary-color', customHex);
                document.documentElement.style.setProperty('--primary-color-2', customHex);
                document.documentElement.style.setProperty('--primary-color-hover', adjustHexBrightness(customHex, 1.15));
                document.documentElement.style.setProperty('--secondary-color', adjustHexBrightness(customHex, 0.80));
            }
            break;
        }
        default:
            break;
    }
}

function ChangeVarsForThemeFont(theme) {
    switch (theme) {
        case themeFont.ssp:
            document.documentElement.classList.add("ssp");
            break;
        default:
            if (document.documentElement.classList.contains("ssp")) document.documentElement.classList.remove("ssp");
            break;
    }
}

export default RobloxApp;
export {
    ChangeVarsForTheme,
    ChangeVarsForThemeColor,
    ChangeVarsForThemeFont,
};