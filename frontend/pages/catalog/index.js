import SharedCatalogPage from "../../components/SharedCatalogPage";

const CatalogPage = () => {
    return <SharedCatalogPage />
}

CatalogPage.getInitialProps = () => {
    return {
        title: 'Catalog - Korone',
    }
}

export default CatalogPage;
