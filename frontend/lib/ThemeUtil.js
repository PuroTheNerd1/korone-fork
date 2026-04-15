import {themeColor, themeFont, themeType} from "../services/theme";

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

export {
    ChangeVarsForTheme,
    ChangeVarsForThemeColor,
    ChangeVarsForThemeFont,
};