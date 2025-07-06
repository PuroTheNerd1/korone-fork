import { useRouter } from "next/router";
import { useSearchParams } from "next/navigation";
import GroupAdmin from "../../components/groupAdmin";

const MyGroupAdmin = () => {
  const params = useSearchParams();

  return <GroupAdmin groupId={parseInt(params.get("gid"), 10)} />
}

export default MyGroupAdmin;