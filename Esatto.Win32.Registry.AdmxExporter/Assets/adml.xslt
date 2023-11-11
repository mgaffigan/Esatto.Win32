<?xml version="1.0" encoding="UTF-8"?>
<?altova_samplexml file:///C:/Users/mgaffigan/source/repos/Itp.AmazonConnect.DesktopClient/Itp.AmazonConnect.DesktopClient/bin/x86/Debug/Itp.AmazonConnect.DesktopClient.settings.xml?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:er="urn:esatto:registry" xmlns="http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions">
	<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>
	
	<xsl:template match="/er:RegistrySettings">
		<policyDefinitionResources revision="1.0" schemaVersion="1.0">
			<displayName>
				<xsl:value-of select="er:AssemblyName" />
			</displayName>
			<description>Auto-generated from Esatto Registry Settings</description>
			<resources>
				<stringTable>
					<string id="SUPPORTED_WindowsVISTA">At least Microsoft Windows</string>
					<xsl:apply-templates select="er:Categories/er:Category" />
					<xsl:for-each select="er:Keys/er:Key/er:Settings/er:Setting[not(er:ParentSettingUuid/text())]">
						<xsl:call-template name="SettingStrings" />
					</xsl:for-each>
				</stringTable>
				<presentationTable>
					<xsl:for-each select="er:Keys/er:Key">
						<xsl:variable name="key" select="." />
						<xsl:for-each select="er:Settings/er:Setting[not(er:ParentSettingUuid/text())]">
							<xsl:variable name="val" select="." />
							<presentation id="p_{er:Uuid}">
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
							</presentation>
						</xsl:for-each>
					</xsl:for-each>
				</presentationTable>
			</resources>
		</policyDefinitionResources>
	</xsl:template>
	
	<xsl:template match="er:Category">
		<string id="cat_dn_{er:Uuid}">
			<xsl:value-of select="er:Name" />
		</string>
	</xsl:template>
		
	<xsl:template name="SettingStrings">
		<string id="value_dn_{er:Uuid}">
			<xsl:value-of select="er:DisplayName" />
		</string>
		<string id="value_exp_{er:Uuid}">
			<xsl:value-of select="er:Description" />
		</string>
	</xsl:template>
	
	<xsl:template name="SimpleValue">
    <!-- This doesn't work since it is a filtered collection -->
		<!--<xsl:if test="position() > 1">
			<text><xsl:text> </xsl:text></text>
		</xsl:if>-->
		<xsl:choose>
			<xsl:when test="er:PropertyType = 'Boolean'">
				<checkBox refId="{er:Uuid}">
					<xsl:if test="er:DefaultValue = 'true'">
						<xsl:attribute name="defaultChecked">true</xsl:attribute>
					</xsl:if>
					<xsl:value-of select="er:DisplayName" />
				</checkBox>
			</xsl:when>
			<xsl:when test="er:PropertyType = 'Int32'">
				<decimalTextBox refId="{er:Uuid}">
					<xsl:if test="er:DefaultValue/text()">
						<xsl:attribute name="defaultValue">
							<xsl:value-of select="er:DefaultValue" />
						</xsl:attribute>
					</xsl:if>
					<xsl:value-of select="er:DisplayName" />
				</decimalTextBox>
			</xsl:when>
			<xsl:when test="er:PropertyType = 'String'">
				<textBox refId="{er:Uuid}">
					<label>
						<xsl:value-of select="er:DisplayName" />
					</label>
					<xsl:if test="er:DefaultValue/text()">
						<defaultValue>
							<xsl:value-of select="er:DefaultValue" />
						</defaultValue>
					</xsl:if>
				</textBox>
			</xsl:when>
			<xsl:when test="er:PropertyType = 'String[]'">
				<multiTextBox refId="{er:Uuid}">
					<xsl:value-of select="er:DisplayName" />
				</multiTextBox>
			</xsl:when>
			<xsl:otherwise>
				<!--<xsl:message terminate="yes">Unknown PropertyType</xsl:message>-->
        <textBox refId="{er:Uuid}">
          <label>
            <xsl:value-of select="er:DisplayName" />
          </label>
          <xsl:if test="er:DefaultValue/text()">
            <defaultValue>
              <xsl:value-of select="er:DefaultValue" />
            </defaultValue>
          </xsl:if>
        </textBox>
			</xsl:otherwise>
		</xsl:choose>
		<xsl:if test="er:DefaultValue/text()">
			<text>
				<xsl:text>Default: </xsl:text>
				<xsl:value-of select="er:DefaultValue" />
			</text>
		</xsl:if>
		<xsl:if test="er:ParentSettingUuid/text() and er:Description/text()">
			<text>
				<xsl:value-of select="er:Description" />
			</text>
		</xsl:if>
	</xsl:template>
	
	<xsl:template name="ParentedValue">
		<xsl:param name="key"/>
		<xsl:param name="val"/>
		<xsl:param name="children"/>
		<xsl:for-each select="$val">
			<xsl:call-template name="SimpleValue"/>
		</xsl:for-each>
		<xsl:for-each select="$children">
			<xsl:call-template name="SimpleValue"/>
		</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
