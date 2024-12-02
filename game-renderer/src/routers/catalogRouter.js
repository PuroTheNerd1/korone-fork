import route from 'ultimate-express'
import { RequestHatThumbnail, RequestHeadRender, RequestMeshThumbnail, RequestPackageThumbnail } from '../controllers/catalogRCC.js'
import authMiddleware from '../middleware/auth.js'

const router = route.Router();
router.post(`/hat`, authMiddleware, RequestHatThumbnail);
router.post(`/package`, authMiddleware, RequestPackageThumbnail);
router.post(`/mesh`, authMiddleware, RequestMeshThumbnail);
router.post(`/head`, authMiddleware, RequestHeadRender);

export default router;