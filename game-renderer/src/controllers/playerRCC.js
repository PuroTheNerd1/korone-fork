import conf from "../util/config.js";
import request from "../util/request.js";
import enums from "../util/enums.js";
import joi from "joi";
import xml2js from "xml2js";
import responseUtil from "../util/response.js";
import AvatarTemplate from "../../scripts/Avatar.json" with { type: "json" };
import HeadshotTemplate from "../../scripts/Closeup.json" with { type: "json" };
const port0 = enums.PlayerRCC[0];
const port1 = enums.PlayerRCC[1];

const schema = joi.object({
    userId: joi.number().required().integer(),
    jobExpiration: joi.number().max(60).default(20).integer(),
});

const queue0 = [];
const queue1 = [];
let isRendering0 = false;
let isRendering1 = false;

const processQueue = (selectedPort) => {
    if (selectedPort === port0) {
        if (queue0.length === 0) {
            isRendering0 = false;
            return;
        }
        isRendering0 = true;
        const { task, resolve, reject } = queue0.shift();
        task(port0)
            .then((result) => {
                resolve(result);
                processQueue(port0);
            })
            .catch((err) => {
                reject(err);
                processQueue(port0);
            });
    } else {
        if (queue1.length === 0) {
            isRendering1 = false;
            return;
        }
        isRendering1 = true;
        const { task, resolve, reject } = queue1.shift();
        task(port1)
            .then((result) => {
                resolve(result);
                processQueue(port1);
            })
            .catch((err) => {
                reject(err);
                processQueue(port1);
            });
    }
};

const enqueueRender = (task) => {
    return new Promise((resolve, reject) => {
        if (!isRendering0 && queue0.length === 0) {
            queue0.push({ task, resolve, reject });
            processQueue(port0);
        } else if (!isRendering1 && queue1.length === 0) {
            queue1.push({ task, resolve, reject });
            processQueue(port1);
        } else {
            if (queue0.length <= queue1.length) {
                queue0.push({ task, resolve, reject });
            } else {
                queue1.push({ task, resolve, reject });
            }
        }
    });
};

const handleRequest = async (req, res, template, width, height, currentPort) => {
    try {
        const { error } = schema.validate(req.body);
        if (error) {
            throw new Error("Invalid form");
        }
        let { userId, jobExpiration } = req.body;
        if (jobExpiration == undefined) { jobExpiration = 20; }
        const characterAppearanceUrl = `${conf.baseUrl}/v1.1/avatar-fetch?placeId=0&userId=${userId}`;
        const xml = JSON.parse(JSON.stringify(template));
        xml.Settings.Arguments[0] = conf.baseUrl;
        xml.Settings.Arguments[1] = characterAppearanceUrl;
        xml.Settings.Arguments[3] = width;
        xml.Settings.Arguments[4] = height;
        const response = await request({
            RCC: currentPort,
            XML: xml,
            jobExpiration,
        });
        return new Promise((resolve, reject) => {
            xml2js.parseString(response.data, (err, jsXmlData) => {
                if (err) return reject(err);
                try {
                    const xmlData =
                        jsXmlData["SOAP-ENV:Envelope"]["SOAP-ENV:Body"][0]["ns1:BatchJobExResponse"][0]["ns1:BatchJobExResult"][0]["ns1:value"][0];
                    console.log(`[info] Rendered on port ${currentPort} successfully`);
                    resolve(responseUtil(res, "success", 200, true, { data: xmlData }));
                } catch (e) {
                    reject(e);
                }
            });
        });
    } catch (err) {
        if (currentPort === port0) {
            console.log(`[error] Render on port ${port0} failed, retrying on port ${port1}`);
            return await handleRequest(req, res, template, width, height, port1);
        } else {
            console.log(`[error] Render on port ${port1} failed, retrying on port ${port0}`);
            return await handleRequest(req, res, template, width, height, port0);
        }
    }
};

export const RequestAvatarThumbnail = async (req, res) => {
    return await enqueueRender((selectedPort) =>
        handleRequest(req, res, AvatarTemplate, 840, 840, selectedPort)
    );
};

export const RequestAvatarHeadshot = async (req, res) => {
    return await enqueueRender((selectedPort) =>
        handleRequest(req, res, HeadshotTemplate, 720, 720, selectedPort)
    );
};