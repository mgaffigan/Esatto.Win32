<?xml version="1.0" encoding="UTF-8"?>
<?altova_samplexml file:///C:/Users/mgaffigan/source/repos/Itp.AmazonConnect.DesktopClient/Itp.AmazonConnect.DesktopClient/bin/x86/Debug/Itp.AmazonConnect.DesktopClient.settings.xml?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:er="urn:esatto:registry" xmlns="http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions">
  <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
  <xsl:template match="/er:RegistrySettings">
    <policyDefinitions revision="1.0" schemaVersion="1.0">
      <policyNamespaces>
        <target prefix="tns" namespace="{er:AssemblyName}"/>
      </policyNamespaces>
      <resources minRequiredRevision="1.0"/>
      <supportedOn>
        <definitions>
          <definition name="SUPPORTED_WindowsVISTA" displayName="$(string.SUPPORTED_WindowsVISTA)" />
        </definitions>
      </supportedOn>
      <categories>
        <xsl:for-each select="er:Categories/er:Category">
          <category name="{er:Uuid}" displayName="$(string.cat_dn_{er:Uuid})">
            <xsl:if test="er:ParentUuid/text()">
              <parentCategory ref="{er:ParentUuid}" />
            </xsl:if>
          </category>
        </xsl:for-each>
      </categories>
      <policies>
        <xsl:apply-templates select="er:Keys/er:Key" />
      </policies>
    </policyDefinitions>
  </xsl:template>

  <xsl:template match="er:Key">
    <xsl:variable name="key" select="." />
    <xsl:for-each select="er:Settings/er:Setting[not(er:ParentSettingUuid/text())]">
      <xsl:variable name="val" select="." />
      <policy name="{er:Uuid}" class="Both" key="{$key/er:PolicyPath}" valueName="Policy_{er:Uuid}"
				presentation="$(presentation.p_{er:Uuid})"
				displayName="$(string.value_dn_{er:Uuid})" explainText="$(string.value_exp_{er:Uuid})">
        <parentCategory ref="{$key/er:Category/er:Uuid}" />
        <supportedOn ref="SUPPORTED_WindowsVISTA" />
        <elements>
          <xsl:choose>
            <xsl:when test="$key/er:Settings/er:Setting[er:ParentSettingUuid = $val/er:Uuid]">
              <xsl:call-template name="ParentedValue">
                <xsl:with-param name="key" select="$key" />
                <xsl:with-param name="val" select="$val" />
                <xsl:with-param name="children" select="$key/er:Settings/er:Setting[er:ParentSettingUuid = $val/er:Uuid]" />
              </xsl:call-template>
            </xsl:when>
            <xsl:otherwise>
              <xsl:call-template name="SimpleValue" />
            </xsl:otherwise>
          </xsl:choose>
        </elements>
      </policy>
    </xsl:for-each>
  </xsl:template>

  <xsl:template name="SimpleValue">
    <xsl:choose>
      <xsl:when test="er:PropertyType = 'Boolean'">
        <boolean id="{er:Uuid}" valueName="{er:Name}">
          <trueValue>
            <decimal value="1" />
          </trueValue>
          <falseValue>
            <decimal value="0" />
          </falseValue>
        </boolean>
      </xsl:when>
      <xsl:when test="er:PropertyType = 'Int32'">
        <decimal id="{er:Uuid}" valueName="{er:Name}" required="true" />
      </xsl:when>
      <xsl:when test="er:PropertyType = 'String'">
        <text id="{er:Uuid}" valueName="{er:Name}" />
      </xsl:when>
      <xsl:when test="er:PropertyType = 'String[]'">
        <multiText id="{er:Uuid}" valueName="{er:Name}" />
      </xsl:when>
      <xsl:otherwise>
        <text id="{er:Uuid}" valueName="{er:Name}" />
        <!--<xsl:message terminate="yes">Unknown PropertyType</xsl:message>-->
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="ParentedValue">
    <xsl:param name="key" />
    <xsl:param name="val" />
    <xsl:param name="children" />

    <xsl:for-each select="$val">
      <xsl:call-template name="SimpleValue" />
    </xsl:for-each>
    <xsl:for-each select="$children">
      <xsl:call-template name="SimpleValue" />
    </xsl:for-each>

  </xsl:template>
</xsl:stylesheet>
