import {useRouter} from "next/router";
import ForumSubcategory from "../../components/forumSubcategory";
import ForumPostReply from "../../components/forumPostReply";
import ForumThreadCreate from "../../components/forumThreadCreate";
import { useSearchParams } from "next/navigation";

const AddPostPage = props => {
  const router = useRouter();
  const params = useSearchParams();

  const id = params.get("PostID");
  const subId = params.get("ForumID");
  if (subId)
    return <ForumThreadCreate id={subId} />
  if (id)
    return <ForumPostReply id={id} />

  return null;
}

export default AddPostPage;