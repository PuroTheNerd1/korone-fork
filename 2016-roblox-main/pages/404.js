import NotFound from "../components/notFound";
import Theme2016 from "../components/theme2016";

const NotFoundPage = () => {
  return <Theme2016>
    <NotFound />
  </Theme2016>
}

export default NotFoundPage;

export const getStaticProps = () => {
  return {
    props: {
      title: '404 - Pekora',
    },
  };
};
