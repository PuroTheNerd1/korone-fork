import NewTradePage from "../../../components/newTradePage";

const UserTradeRoute = () => {
  return <NewTradePage />;
};

UserTradeRoute.getInitialProps = () => {
  return {
    title: "Trade - Korone",
  };
};

export default UserTradeRoute;
