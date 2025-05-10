import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import joi from "joi";
import responseUtil from "../util/response.js";
import { enqueue } from "../util/rccQueue.js";
import { parseXml } from "../util/xmlUtil.js";

import HatTemplate from '../../scripts/Hat.json' with { type: 'json' };
import PackageTemplate from '../../scripts/Package.json' with { type: 'json' };
import BodyPartTemplate from '../../scripts/BodyPart.json' with { type: 'json' };
import MeshTemplate from '../../scripts/Mesh.json' with { type: 'json' };
import MeshPartTemplate from '../../scripts/MeshPart.json' with { type: 'json' };
import HeadTemplate from '../../scripts/Head.json' with { type: 'json' };
import ModelTemplate from '../../scripts/Model.json' with { type: 'json' };
import AnimationSilhouetteTemplate from '../../scripts/AnimationSilhouette.json' with { type: 'json' };
import AnimationTemplate from '../../scripts/AvatarAnimation.json' with { type: 'json' };

const RCC_PORT = enums.CatalogRCC;
const MAX_JOB_EXPIRATION = 60;
const DEFAULT_JOB_EXPIRATION = 20;

function validate(schema, payload) {
  const { error } = schema.validate(payload);
  if (error) throw new Error(`Invalid form: ${error.message}`);
}

async function runRender(xmlTemplate, argsSetter, jobExpiration) {
  const xml = JSON.parse(JSON.stringify(xmlTemplate));
  argsSetter(xml);
  const response = await request({ RCC: RCC_PORT, XML: xml, jobExpiration });
  return parseXml(response.data);
}

export const RequestHatThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        assetId: joi.number().integer().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetId, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;
    const assetUrl = `${conf.baseUrl}asset?id=${assetId}`;

    const data = await enqueue(() =>
      runRender(HatTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[4] = conf.baseUrl;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestAnimationSilhouetteThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        assetId: joi.number().integer().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetId, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;
    const assetUrl = `${conf.baseUrl}asset?id=${assetId}`;

    const data = await enqueue(() =>
      runRender(AnimationSilhouetteTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[4] = "128/128/128";
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestAnimationThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        characterAppearanceUrl: joi.string().required(),
        animationUrl: joi.string().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { characterAppearanceUrl, animationUrl, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;

    const data = await enqueue(() =>
      runRender(AnimationTemplate, xml => {
        xml.Settings.Arguments[0] = characterAppearanceUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[5] = animationUrl;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestModelThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        assetId: joi.number().integer().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetId, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;
    const assetUrl = `${conf.baseUrl}asset?id=${assetId}`;

    const data = await enqueue(() =>
      runRender(ModelTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[4] = conf.baseUrl;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestBodyPartThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        assetUrl: joi.string().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetUrl, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;

    const data = await enqueue(() =>
      runRender(BodyPartTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = 1680;
        xml.Settings.Arguments[5] = `${conf.baseUrl}asset/?id=1785197`;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestPackageThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        assetUrls: joi.string().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetUrls, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;

    const data = await enqueue(() =>
      runRender(PackageTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrls;
        xml.Settings.Arguments[1] = conf.baseUrl;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = 1680;
        xml.Settings.Arguments[5] = `${conf.baseUrl}asset/?id=1785197`;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestMeshThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        assetId: joi.number().integer().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetId, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;
    const assetUrl = `${conf.baseUrl}asset?id=${assetId}`;

    const data = await enqueue(() =>
      runRender(MeshTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1260;
        xml.Settings.Arguments[3] = 1260;
        xml.Settings.Arguments[4] = conf.baseUrl;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestMeshPartThumbnail = async (req, res) => {
  try {
    validate(
      joi.object({
        assetId: joi.number().integer().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetId, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;
    const assetUrl = `${conf.baseUrl}v1/asset?id=${assetId}`;

    const data = await enqueue(() =>
      runRender(MeshPartTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1260;
        xml.Settings.Arguments[3] = 1260;
        xml.Settings.Arguments[4] = conf.baseUrl;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};

export const RequestHeadRender = async (req, res) => {
  try {
    validate(
      joi.object({
        assetId: joi.number().integer().required(),
        jobExpiration: joi.number().integer().max(MAX_JOB_EXPIRATION).default(DEFAULT_JOB_EXPIRATION),
      }),
      req.body
    );
    const { assetId, jobExpiration = DEFAULT_JOB_EXPIRATION } = req.body;
    const assetUrl = `${conf.baseUrl}asset?id=${assetId}`;

    const data = await enqueue(() =>
      runRender(HeadTemplate, xml => {
        xml.Settings.Arguments[0] = assetUrl;
        xml.Settings.Arguments[2] = 1680;
        xml.Settings.Arguments[3] = 1680;
        xml.Settings.Arguments[4] = conf.baseUrl;
        xml.Settings.Arguments[5] = 1785197;
      }, jobExpiration)
    );
    return responseUtil(res, "success", 200, true, { data });
  } catch (err) {
    return responseUtil(res, err.message.startsWith("Invalid") ? "Invalid form" : "An internal server error occurred.", err.message.startsWith("Invalid") ? 400 : 500, false, { error: err.message });
  }
};