import { v4 as uuidv4 } from 'uuid';
import conf from './config.js';

export const soap = (baseUrl, jobExpiration, finalScript) => {
    return `
    <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:rob="${conf.baseUrl}">
   <soapenv:Header/>
   <soapenv:Body>
      <rob:BatchJob>
         <rob:job>
            <rob:id>${uuidv4().toString()}</rob:id>
            <rob:expirationInSeconds>${jobExpiration}</rob:expirationInSeconds>
            <rob:cores>1</rob:cores>
         </rob:job>
         <rob:script>
            <rob:name>${uuidv4().toString()}</rob:name>
            <rob:script><![CDATA[
                ${finalScript}
            ]]></rob:script>
            ${/*<arguments>
                {Arguments}
            </arguments>*/null}
         </rob:script>
      </rob:BatchJob>
   </soapenv:Body>
</soapenv:Envelope>
`
    /*return `<?xml version=""1.0"" encoding=""utf-8""?>
    <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:rob="http://roblox.com/">
   <soapenv:Header/>
   <soapenv:Body>
      <BatchJob>
         <job>
            <id>${uuidv4().toString()}</id>
            <expirationInSeconds>${jobExpiration}</expirationInSeconds>
            <cores>1</cores>
         </job>
         <script>
            <name>${uuidv4().toString()}</name>
            <script><![CDATA[
                ${finalScript}
            ]]></script>
            ${/*<arguments>
                {Arguments}
            </arguments>
         </script>
      </BatchJob>
   </soapenv:Body>
</soapenv:Envelope>
`*/
    /*return `<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <BatchJob xmlns=""${baseUrl}"">
                        <job>
                            <id>${uuidv4().toString()}</id>
                            <category>1</category>
                            <cores>1</cores>
                            <expirationInSeconds>${jobExpiration}</expirationInSeconds>
                        </job>
                        <script>
                            <name>${uuidv4().toString()}</name>
                            <script>
                                <![CDATA[
                                ${finalScript}
                                ]]>
                            </script>
                        </script>
                    </BatchJob>
                </soap:Body>
            </soap:Envelope>`*/
}