import {createUseStyles} from "react-jss";
import CatalogPageStore, { CatalogCategory, FormatCamelCase } from "../stores/CatalogPageStore";
import Selector from "../../selector";
import ActionButton from "../../actionButton";
import useButtonStyles from "../../../styles/buttonStyles";
import {tick, wait} from "../../../lib/utils";
import {useEffect} from "react";

const useStyles = createUseStyles({
    inputStyle: {
        width: 300,
        borderTopRightRadius: 0,
        borderBottomRightRadius: 0,
        fontWeight: 500,
        color: "#191919",
    },
    selectorWrapper: {
        width: 200,
    },
    selector: {
        padding: "5px 12px",
        borderRadius: 0,
        borderLeft: "none",
        borderColor: "var(--text-color-secondary)",
        "& *": {
            lineHeight: "26px",
            fontWeight: 500,
        },
    },
    searchButton: {
        padding: 4,
        borderTopLeftRadius: 0,
        borderBottomLeftRadius: 0,
        borderLeft: 0,
    },
});

function CatalogInputs() {
    const s = useStyles();
    const store = CatalogPageStore.useContainer();
    const buttonStyles = useButtonStyles();

    useEffect(async () => {
        await tick()
        await store.RefreshCatalogItems(null, true)
    }, []);
    
    return <div className="flex">
        <input
            type="text"
            placeholder="Search"
            onChange={e => store.setSearchInput(e.target.value)}
            className={`inputTextStyle ${store.searchInvalid ? "hasError" : ""} ${s.inputStyle}`}
            maxLength={100}
            onKeyPress={e => {
                if (e.key === "Enter")
                    store.RefreshCatalogItems(e, true);
            }}
        />
        <div className={`flex`}>
            <Selector
                onChange={async newValue => {
                    if (store.refreshDebounce.current || store.category === newValue.value) return false;
                    store.setCategory(newValue.value);
                    await tick();
                    store.RefreshCatalogItems(null, true, { category: newValue.value });
                }}
                options={Object.keys(CatalogCategory).map(key => (
                    {
                        name: FormatCamelCase(key),
                        value: CatalogCategory[key],
                    }
                ))}
                wrapperClass={s.selectorWrapper}
                className={s.selector}
            />
            <ActionButton
                className={s.searchButton}
                buttonStyle={buttonStyles.newCancelButton}
                onClick={e => store.RefreshCatalogItems(e, true)}
            >
                <div className="flex justify-content-center align-items-center">
                    <span className="icon-search"/>
                </div>
            </ActionButton>
        </div>
    </div>
}

export default CatalogInputs;
