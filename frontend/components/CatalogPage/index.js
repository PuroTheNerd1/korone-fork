import {createUseStyles} from "react-jss";
import Link from "../link";
import CatalogInputs from "./components/CatalogInputs";
import CatalogResults from "./components/CatalogResults";
import CatalogPageStore from "./stores/CatalogPageStore";
import {getTheme, themeType} from "../../services/theme";
import dynamic from "next/dynamic";
import {useEffect} from "react";
import {useRouter} from "next/dist/client/router";
import {IsValidNum} from "../../lib/utils";

const useStyles = createUseStyles({
    searchOptionsContainer: {
        width: "160px",
        borderRight: "1px solid var(--text-color-secondary)",
        marginRight: 12,
        "@media(max-width: 576px)": {
            position: "absolute",
            top: 0,
            left: -160,
            zIndex: 990,
            backgroundColor: "var(--white-color)",
            marginRight: 0,
            border: "none",
            boxShadow: p => p.theme === themeType.dark ? "0 1px 4px 0 rgba(230,230,230,0.3)" : "0 1px 4px 0 rgba(25,25,25,0.3)",
            transition: "left 0.3s ease",
            "&.visible": {
                left: 0,
            },
        },
    },
    searchResultsContainer: {
        width: "calc(100% - 172px)",
        "@media(max-width: 576px)": {
            width: "100%",
        },
    },
    catalogHeader: {
        width: "100%",
        "@media(max-width: 767px)": {
            flexDirection: "column",
            marginBottom: 20,
            gap: 10,
            "& > div": {
                width: "100%",
                padding: "0 8px",
            },
            "& h1": {
                fontSize: "52px!important",
            },
        },
        "@media(max-width: 576px)": {
            marginBottom: 4,
        },
    },
    catalogContainer: {
        "@media(max-width: 767px)": {
            padding: "0 8px",
        },
    },
    catalogPage: {
        background: p => p.theme === themeType.obc2019 ? "#e3e3e3" : "transparent",
        padding: p => p.theme === themeType.obc2019 ? "0 6px" : "0",
        borderRadius: p => p.theme === themeType.obc2019 ? "3px" : "0",
    },
});

function CatalogPage() {
    const s = useStyles({theme: getTheme()});
    const router = useRouter();
    const store = CatalogPageStore.useContainer();
    const CatalogNavigation = dynamic(() => import("./components/CatalogNavigation"), { ssr: false })
    const CatalogFilters = dynamic(() => import("./components/CatalogFilters"), { ssr: false })

    useEffect(() => {
        let creatorName = router.query['CreatorName'] ?? null;
        let creatorType = router.query['CreatorType'] ?? null;
        let category = router.query['Category'] ?? null;

        let options = store.options;
        if (creatorName && typeof creatorType === 'string') options.creator = creatorName;
        if (creatorType) {
            if (IsValidNum(creatorType)) {
                options.creatorOption = Number.parseInt(creatorType);
            } else if (typeof creatorType === 'string') {
                switch (creatorType.toLowerCase()) {
                    case "user": {
                        options.creatorOption = 1;
                        break;
                    }
                    case "group": {
                        options.creatorOption = 2;
                        break;
                    }
                }
            }
        }
        if (category && IsValidNum(category)) {
            options.category = Number.parseInt(category);
            if (Number.parseInt(category) === 1) {
                options.subCategory = 0;
            }
        }
    }, [router.query]);

    return <div className={`w-100 flex flex-column ${s.catalogPage}`}>
        <div className={`w-100 flex justify-content-between align-items-center ${s.catalogHeader}`}>
            <h1 style={{ fontSize: 36, fontWeight: 800, }}>
                <Link href="/catalog">
                    <a href="/catalog" className="inherit-color inherit-font-size">Catalog</a>
                </Link>
            </h1>
            <CatalogInputs />
        </div>
        <div className={`w-100 flex ${s.catalogContainer} position-relative`}>
            <div className={`${s.searchOptionsContainer} ${store.navVisible ? "visible" : ""}`}>
                <CatalogNavigation />
                <CatalogFilters />
            </div>
            <div className={s.searchResultsContainer}>
                <CatalogResults />
            </div>
        </div>
    </div>
}

export default CatalogPage;
