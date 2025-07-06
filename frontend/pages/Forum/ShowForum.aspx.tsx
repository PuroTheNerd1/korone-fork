import {useRouter} from "next/router";
import ForumSubcategory from "../../components/forumSubcategory";
import { useSearchParams } from "next/navigation";

const ShowForumPage = props => {
  const router = useRouter();
  const params = useSearchParams();

  const id = params.get("ForumID");
  const page = parseInt(params.get("Page"), 10);
  if (!id)
    return null;
  return <ForumSubcategory id={id} page={Number.isInteger(page) && page > 0 ? page : 1} />
}

export default ShowForumPage;