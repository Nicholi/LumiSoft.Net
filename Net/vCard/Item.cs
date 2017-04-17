using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCard structure item.
    /// </summary>
    public class Item
    {
        private vCard  m_pCard      = null;
        private string m_Name       = "";
        private string m_Parameters = "";
        private string m_Value      = "";
        private bool   m_FoldData   = true;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="card">Owner card.</param>
        /// <param name="name">Item name.</param>
        /// <param name="parameters">Item parameters.</param>
        /// <param name="value">Item encoded value value.</param>
        internal Item(vCard card,string name,string parameters,string value)
        {
            m_pCard      = card;
            m_Name       = name;
            m_Parameters = parameters;
            m_Value      = value;
        }


        #region method SetDecodedValue

        /// <summary>
        /// Sets item decoded value. Value will be encoded as needed and stored to item.Value property.
        /// </summary>
        /// <param name="value"></param>
        public void SetDecodedValue(string value)
        {
            /* RFC 2426 vCrad 5. Differences From vCard v2.1
                The QUOTED-PRINTABLE inline encoding has been eliminated.
                Only the "B" encoding of [RFC 2047] is an allowed value for
                the ENCODING parameter.
              
                The CRLF character sequence in a text type value is specified 
                with the backslash character sequence "\n" or "\N".
             
                Any COMMA or SEMICOLON in a text type value must be backslash escaped.
            */
            
            // Remove encoding and charset parameters
            string newParmString = "";
            string[] parameters = m_Parameters.ToLower().Split(';');
            foreach(string parameter in parameters){
                string[] name_value = parameter.Split('=');
                if(name_value[0] == "encoding" || name_value[0] == "charset"){                        
                }
                else if(parameter.Length > 0){
                    newParmString += parameter + ";";
                }
            }            

            if(m_pCard.Version.StartsWith("3")){
                // Add encoding parameter
                if(!Net_Utils.IsAscii(value)){
                    newParmString += "CHARSET=utf-8";
                }

                this.ParametersString = newParmString;
                this.Value = vCard_Utils.Encode(m_pCard.Version,m_pCard.Charset,value);
            }
            else{                
                if(NeedEncode(value)){
                    // Add encoding parameter
                    newParmString += "ENCODING=QUOTED-PRINTABLE;CHARSET=" + m_pCard.Charset.WebName;

                    this.ParametersString = newParmString;
                    this.Value = vCard_Utils.Encode(m_pCard.Version,m_pCard.Charset,value);
                }
                else{
                    this.ParametersString = newParmString;
                    this.Value = value;
                }
            }
        }

        #endregion


        #region internal method ToItemString

        /// <summary>
        /// Converts item to vCal item string.
        /// </summary>
        /// <returns></returns>
        internal string ToItemString()
        {
            string value = m_Value;
            if(m_FoldData){
                value = FoldData(value);
            }

            if(m_Parameters.Length > 0){
                return m_Name + ";" + m_Parameters + ":" + value;
            }
            else{
                return m_Name + ":" + value;
            }
        }

        #endregion


        #region method NeedEncode

        /// <summary>
        /// CHecks if specified value must be encoded.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <returns>Returns true if value must be encoded, otherwise false.</returns>
        private bool NeedEncode(string value)
        {
            // We have 8-bit chars.
            if(!Net_Utils.IsAscii(value)){
                return true;
            }

            // Allow only printable chars and whitespaces.
            foreach(char c in value){
                if(char.IsControl(c)){
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region static method FoldData

        /// <summary>
        /// Folds long data line to folded lines.
        /// </summary>
        /// <param name="data">Data to fold.</param>
        /// <returns></returns>
        private string FoldData(string data)
        {
            /* Folding rules:
                *) Line may not be bigger than 76 chars.
                *) If possible fold between TAB or SP
                *) If no fold point, just fold from char 76
            */

            // Data line too big, we need to fold data.
            if(data.Length > 76){
                int startPosition       = 0;
                int lastPossibleFoldPos = -1;
                StringBuilder retVal = new StringBuilder();
                for(int i=0;i<data.Length;i++){
                    char c = data[i];
                    // We have possible fold point
                    if(c == ' ' || c == '\t'){
                        lastPossibleFoldPos = i;
                    }

                    // End of data reached
                    if(i == (data.Length - 1)){
                        retVal.Append(data.Substring(startPosition));
                    }
                    // We need to fold
                    else if((i - startPosition) >= 76){
                        // There wasn't any good fold point(word is bigger than line), just fold from current position.
                        if(lastPossibleFoldPos == -1){
                            lastPossibleFoldPos = i;
                        }
                    
                        retVal.Append(data.Substring(startPosition,lastPossibleFoldPos - startPosition) + "\r\n\t");

                        i = lastPossibleFoldPos;
                        lastPossibleFoldPos = -1;
                        startPosition       = i;
                    }
                }

                return retVal.ToString();
            }
            else{
                return data;
            }
        }

        #endregion
        

        #region Properties Implementation

        /// <summary>
        /// Gest item name.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets or sets item parameters.
        /// </summary>
        public string ParametersString
        {
            get{ return m_Parameters; }

            set{ m_Parameters = value; }
        }

        /// <summary>
        /// Gets or sets item encoded value. NOTE: If you set this property value, you must encode data 
        /// by yourself and also set right ENCODING=encoding; and CHARSET=charset; prameter in item.ParametersString !!!
        /// Normally use method item.SetDecodedStringValue method instead, this does all you need.
        /// </summary>
        public string Value
        {
            get{ return m_Value; }

            set{ m_Value = value; }
        }

        /// <summary>
        /// Gets item decoded value. If param string specifies Encoding and/or Charset, 
        /// item.Value will be decoded accordingly.
        /// </summary>
        public string DecodedValue
        {
            /* RFC 2426 vCrad 5. Differences From vCard v2.1              
                The CRLF character sequence in a text type value is specified 
                with the backslash character sequence "\n" or "\N".
             
                Any COMMA or SEMICOLON in a text type value must be backslash escaped.
            */

            get{ 
                string data     = m_Value;
                string encoding = null;
                string charset  = null;
                string[] parameters = m_Parameters.ToLower().Split(';');
                foreach(string parameter in parameters){
                    string[] name_value = parameter.Split('=');
                    if(name_value[0] == "encoding" && name_value.Length > 1){
                        encoding = name_value[1];
                    }
                    else if(name_value[0] == "charset" && name_value.Length > 1){
                        charset = name_value[1];
                    }
                }

                // Encoding specified, decode data.
                if(encoding != null){
                    if(encoding == "quoted-printable"){
                        data = Helpers.GetDefaultEncoding().GetString(MIME_Utils.QuotedPrintableDecode(Helpers.GetDefaultEncoding().GetBytes(data)));
                    }
                    else if(encoding == "b" || encoding == "base64"){
                        data = Helpers.GetDefaultEncoding().GetString(Net_Utils.FromBase64(Helpers.GetDefaultEncoding().GetBytes(data)));
                    }
                    else{
                        throw new Exception("Unknown data encoding '" + encoding + "' !");
                    }
                }
                // Charset specified, convert data to specified charset.
                if(charset != null){
                    data = System.Text.Encoding.GetEncoding(charset).GetString(Helpers.GetDefaultEncoding().GetBytes(data));
                }

                // FIX ME: this must be done with structured fields
                //data = data.Replace("\\n","\r\n");
                //data = TextUtils.UnEscapeString(data); Messes up structured fields

                return data; 
            }
        }

        /// <summary>
        /// Gets or sets if long data lines are foolded.
        /// </summary>
        public bool FoldLongLines
        {
            get{ return m_FoldData; }

            set{ m_FoldData = value; }
        }


        /// <summary>
        /// Gets owner.
        /// </summary>        
        internal vCard Owner
        {
            get{ return m_pCard; }
        }

        #endregion

    }
}
