import route from 'ultimate-express'
import { RequestImageThumbnail, RequestClothingThumbnail, RequestTeeShirtThumbnail } from '../controllers/imageRCC.js'
import authMiddleware from '../middleware/auth.js'

const router = route.Router();

router.post(`/image`, authMiddleware, RequestImageThumbnail);
router.post(`/clothing`, authMiddleware, RequestClothingThumbnail);
router.post(`/teeshirt`, authMiddleware, RequestTeeShirtThumbnail);

export default router;