import Head from 'next/head';
import Theme2016 from "../../../components/theme2016";
import UserProfile from "../../../components/userProfile";
import UserProfileStore from "../../../components/userProfile/stores/UserProfileStore";
import { getUserInfo } from "../../../services/users";

const UserProfilePage = ({ username, userId }) => {
  const ogTitle = username || "Project X";
  const ogUrl = userId ? `https://projex.zip/users/${userId}/profile` : '';
  const ogDesc = description || "";

  return (
    <>
      <Head>
        <title>{username}</title>
        <meta property="og:title" content={ogTitle} />
        <meta property="og:url" content={ogUrl} />
        <meta property="og:type" content="profile" />
        <meta property="og:description" content="" />
      </Head>
      <UserProfileStore.Provider>
        <Theme2016>
          <UserProfile userId={userId}/>
        </Theme2016>
      </UserProfileStore.Provider>
    </>
  );
};

export async function getServerSideProps(context) {
  const { userId } = context.query;

  try {
    const info = await getUserInfo({ userId });
    const username = info.name || "Project X"; 
    return {
      props: {
        username,
        userId
      }
    };
  } catch (error) {
    console.error("Error fetching user info:", error);
    return {
      props: {
        username: "Project X", 
        userId
      }
    };
  }
}

export default UserProfilePage;
