import { createUseStyles } from "react-jss";
import { useEffect, useRef, useState } from "react";
import NewLink from "../../NewLink";
import { wait } from "../../../lib/utils";
import CatalogPageStore from "../stores/CatalogPageStore";

const useStyles = createUseStyles({
    searchOptionWrapper: {
        borderBottom: "1px solid #b8b8b8",
        margin: "0 12px 0 0",
    },
    searchOptionHeader: {
        fontSize: 20,
        fontWeight: 700,
        marginBottom: 4,
        padding: "5px 0",
        lineHeight: "1em",
    },
    categoryWrapper: {
        paddingBottom: 12,
    },
    categoryContainer: {
        marginBottom: 6,
        "& a": {
            fontSize: 12,
            fontWeight: 400,
        },
    },
    subCategoryContainer: {
        paddingLeft: 12,
    },
    subCategoryWrapper: {
        fontSize: 16,
        lineHeight: "1.4em",
        "& > a": {
            display: "inline-flex",
            justifyContent: "space-between",
            alignItems: "center",
            width: "100%",
        },
    },
    collapse: {
        maxHeight: 0,
        // display: "none",
        flexDirection: "column",
        transition: "max-height .35s ease",
        overflow: "hidden",
        "&.in": {
            display: "flex",
            maxHeight: "1000px",
        },
        "&.out": {
            display: "flex",
            maxHeight: 0,
        },
    },
});

function CatalogNavigation() {
    const s = useStyles();
    const store = CatalogPageStore.useContainer();
    
    const { categoryNav } = store;
    const locked = useRef(false);
    const [transition, setTransition] = useState(false);
    // a category can be open if here or if its currently selected in the store
    const [openedCategory, setOpenCategory] = useState(null);
    const [closingCategory, setClosingCategory] = useState(null);
    
    useEffect(async () => {
        await wait(0.4);
        setClosingCategory(null);
    }, [closingCategory]);
    
    return <div className={`${s.categoryWrapper} ${s.searchOptionWrapper} flex flex-column`}>
        <h3 className={s.searchOptionHeader}>Category</h3>
        <div>
            {
                categoryNav?.map(cat => {
                    return <div className={`${s.categoryContainer} flex flex-column`} id={cat.categoryId.toString()}>
                        <div className={s.subCategoryWrapper}>
                            <NewLink className={`link2019-gray`} style={store.category === cat.categoryId || openedCategory === cat.categoryId ? {color: "var(--primary-color)!important"} : {}} onClick={async e => {
                                e.preventDefault();
                                if (locked.current || store.refreshDebounce.current || store.category === cat.categoryId) return;
                                locked.current = true;
                                if (cat.subCategories.length === 0) {
                                    store.setCategory(cat.categoryId);
                                    store.setSubCategory(null);
                                    store.RefreshCatalogItems(null, true, {category: cat.categoryId});
                                    await wait(1);
                                    locked.current = false;
                                    return;
                                }
                                
                                let oldOpenCat = openedCategory === cat.categoryId && store.category !== cat.categoryId ? openedCategory : null;
                                setOpenCategory(openedCategory === cat.categoryId && store.category !== cat.categoryId ? null : cat.categoryId);
                                setClosingCategory(oldOpenCat);
                                setTransition(true);
                                await wait(0.375);
                                setTransition(false);
                                locked.current = false;
                            }}>
                                <span className="inherit-color inherit-font-size">{cat.name}</span>
                                <span
                                    className={`${cat.subCategories.length === 0 ? "display-none" : ""} inherit-color inherit-font-size ${openedCategory === cat.categoryId || store.category === cat.categoryId ? "icon-minus" : "icon-plus"}`}/>
                            </NewLink>
                        </div>
                        
                        <div className={`
                        ${s.subCategoryContainer}
                        ${s.collapse}
                        ${openedCategory === cat.categoryId || store.category === cat.categoryId ? "in" : ""}
                        ${transition && closingCategory === cat.categoryId ? "out" : ""}
                        ${cat.subCategories.length === 0 ? "display-none" : ""}
                        `}
                             // style={transition && openedCategory === cat.categoryId ? { height: "110px" } : {}}
                        >
                            {
                                cat.subCategories.map(sub => {
                                    return <div className={s.subCategoryWrapper}>
                                        <NewLink className={`link2019-gray`} style={store.category === cat.categoryId && store.subCategory === sub.subCategoryId ? {color: "var(--primary-color)!important"} : {}} onClick={async e => {
                                            e.preventDefault();
                                            if (locked.current || store.refreshDebounce.current || store.subCategory === sub.subCategoryId) return;
                                            locked.current = true;
                                            
                                            setOpenCategory(cat.categoryId);
                                            store.setCategory(cat.categoryId);
                                            store.setSubCategory(sub.subCategoryId);
                                            store.RefreshCatalogItems(null, true, { category: cat.categoryId, subCategory: sub.subCategoryId });
                                            
                                            await wait(0.75);
                                            locked.current = false;
                                        }}>{sub.name || sub.subCategory}</NewLink>
                                    </div>
                                })
                            }
                        </div>
                    </div>
                })
            }
        </div>
    </div>
}

export default CatalogNavigation;
