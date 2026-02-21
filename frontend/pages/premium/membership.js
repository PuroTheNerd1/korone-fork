import PremiumMembership from '../../components/premiumMembership';

const MembershipPage = () => {
  return <PremiumMembership />;
};

MembershipPage.getInitialProps = () => {
  return {
    title: 'Upgrade Membership - Korone',
  };
};

export default MembershipPage;
