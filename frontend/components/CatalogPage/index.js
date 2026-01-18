import {createUseStyles} from "react-jss";
import Link from "../link";
import CatalogInputs from "./components/CatalogInputs";
import CatalogNavigation from "./components/CatalogNavigation";
import CatalogResults from "./components/CatalogResults";
import CatalogFilters from "./components/CatalogFilters";

const useStyles = createUseStyles({
    searchOptionsContainer: {
        width: "160px",
        borderRight: "1px solid var(--text-color-secondary)",
        marginRight: 12,
    },
    searchResultsContainer: {
        width: "calc(100% - 172px)",
    },
});

function CatalogPage() {
    const s = useStyles();
    
    return <div className="w-100 flex flex-column">
        <div className="w-100 flex justify-content-between align-items-center">
            <h1 style={{ fontSize: 36, fontWeight: 800, }}>
                <Link href="/catalog">
                    <a href="/catalog" className="inherit-color inherit-font-size">Catalog</a>
                </Link>
            </h1>
            <CatalogInputs />
        </div>
        <div className={`w-100 flex`}>
            <div className={s.searchOptionsContainer}>
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
