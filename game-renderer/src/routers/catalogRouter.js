import route from 'ultimate-express'
import { RequestHatThumbnail, RequestModelThumbnail, RequestAnimationSilhouetteThumbnail, RequestHeadRender, RequestMeshThumbnail, RequestPackageThumbnail, RequestBodyPartThumbnail } from '../controllers/catalogRCC.js'
import authMiddleware from '../middleware/auth.js'

const router = route.Router();
router.post(`/hat`, authMiddleware, RequestHatThumbnail);
router.post(`/model`, authMiddleware, RequestModelThumbnail);
router.post('/animationsilhouette', authMiddleware, RequestAnimationSilhouetteThumbnail);
router.post(`/animation`, authMiddleware, RequestAnimationSilhouetteThumbnail);
router.post(`/package`, authMiddleware, RequestPackageThumbnail);
router.post(`/bodypart`, authMiddleware, RequestBodyPartThumbnail);
router.post(`/mesh`, authMiddleware, RequestMeshThumbnail);
router.post(`/head`, authMiddleware, RequestHeadRender);

export default router;