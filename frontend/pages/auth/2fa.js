import { useEffect } from "react";
import { useRouter } from "next/router";

const TwoFARedirect = () => {
  const router = useRouter();
  useEffect(() => {
    router.replace('/My/Account#security');
  }, []);
  return null;
};

export default TwoFARedirect;
