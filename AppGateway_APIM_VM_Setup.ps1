Connect-AzAccount
$subscriptionID = "<OMITTED>"
Get-AzSubscription -SubscriptionId $subscriptionID | Select-AzSubscription

$rgName = "roriek_crg"
$location = "southcentralus"

$appgatewaysubnet = New-AzVirtualNetworkSubnetConfig -Name "apim01" -AddressPrefix "10.7.0.0/24"
$apimsubnet = New-AzVirtualNetworkSubnetConfig -Name "apim02" -AddressPrefix "10.7.1.0/24"
$vmsubnet = New-AzVirtualnetworkSubnetConfig -Name "apim03" -AddressPrefix "10.7.2.0/24"

$vnet = New-AzVirtualNetwork -Name "appgwvnet" -ResourceGroupName $rgName -Location $location -AddressPrefix "10.7.0.0/16" -Subnet $appgatewaysubnet,$apimsubnet,$vmsubnet

$appgatewaysubnetdata = $vnet.Subnets[0]
$apimsubnetdata = $vnet.Subnets[1]
$vmsubnetdata = $vnet.Subnets[2]

$vmPip = New-AzPublicIpAddress -ResourceGroupName $rgName -Location $location -AllocationMethod Static -IdleTimeoutInMinutes 4 -Name "mypublicdns$(Get-Random)"

$nsgRuleSSH = New-AzNetworkSecurityRuleConfig -Name "myNetworkSecurityGroupRuleSSH" -Protocol "Tcp" -Direction "Inbound" -Priority 1000 -SourceAddressPrefix * -SourcePortRange * -DestinationAddressprefix * -DestinationPortRange 22 -Access "Allow"
$nsgRuleWeb = New-AzNetworkSecurityRuleConfig -Name "myNetworkSecurityGroupRuleWWW" -Protocol "Tcp" -Direction "Inbound" -Priority 1001 -SourceAddressPrefix * -SourcePortRange * -DestinationAddressPrefix * -DestinationPortRange 80 -Access "Allow"
$nsg = New-AzNetworkSecurityGroup -ResourceGroupname $rgName -Location $location -name "myNetworkSecurityGroup" -SecurityRules $nsgRuleSSH, $nsgRuleWeb

$nic = New-AzNetworkInterface -Name "myNic" -ResourceGroupName $rgName -Location $location -SubnetId $vmsubnetdata.Id -PublicIpAddressId $vmPip.Id -NetworkSecurityGroupId $nsg.Id
$pw = "QwErTy!2#4%"
$securePassword = ConvertTo-SecureString $pw -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential ("azureuser", $securePassword)

$vmConfig = New-AzVMConfig -VMName "myVM" -VMSize "Standard_D1" | Set-AzVMOperatingSystem -Linux -ComputerName "myVM" -Credemtial $cred -DisablePasswordAuthentication | Set-AzVMSourceImage -PublisherName "Canonical" -Offer "UbuntuServer" -Skus "16.04-LTS" -Version "Latest" | Add-AzVMNetworkInterface -Id $nic.Id
ssh-keygen -t rsa -b 2048
$sshPublicKey = Get-Content ~/.ssh/id_rsa.pub
Add-AzVMSshPublicKey -VM $vmConfig -KeyData $sshPublicKey -Path "/home/azureuser/.ssh/authorized_keys"

New-AzVM -ResourceGroupName $rgName -VM $vmConfig


$apimVNet = New-AzApiManagementVirtualNetwork -SubnetResourceId $apimsubnetdata.Id

$apimServiceName = "roriek_apim"
$apimOrganization = "Microsoft"
$apimAdminEmail = "v-roriek@microsoft.com"
$apimService = New-AzApiManagement -ResourceGroupName $rgName -Location $location -Name $apimServiceName -Organization $apimOrganization -AdminEmail $apimAdminEmail -VirtualNetwork $apimVNet -VpnType "Internal" -Sku "Developer"

$gatewayhostname = "api.cloudweb.net"
$portalhostname = "portal.cloudweb.net"
$gatewayCertCerPath = "C:\Users\v-roriek\Documents\gateway.cer"
$gatewayCertPfxPath = "C:\Users\v-roriek\Documents\gateway.pfx"

$portalCertPfxPath = "C:\Users\v-roriek\Documents\portal.pfx"

$proxyHostnameConfig = New-AzApiManagementCustomHostnameConfiguration -Hostname $gatewayhostname -HostnameType Proxy -PfxPath $gatewayCertPfxPath -PfxPassword $securePassword
$portalHostnameConfig = New-AzApiManagementCustomHostnameConfiguration -Hostname $portalHostname -HostnameType Portal -PfxPath $portalCertPfxPath -PfxPassword $securePassword

$apimService.ProxyCustomHostnameConfiguration = $proxyHostnameConfig
$apimService.PortalCustomHostnameConfiguration = $portalHostnameConfig
Set-AzApiManagement -InputObject $apimService
$apimPip = New-AzPublicIpAddress -ResourceGroupName $rgName -Name "mypublicdns$(Get-Random)" -Location $location -AllocationMethod Dynamic

$gipConfig = New-AzApplicationGatewayIPConfiguration -Name "gatewayIP01" -Subnet $appgatewaysubnetdata
$fp01 = New-AzApplicationGatewayFrontendPort -Name "Port01" -Port 443
$fipconfig01 = New-AzApplicationgatewayFrontendIPConfig -Name "frontend01" -PublicIpAddress $apimPip

$cert = New-AzApplicationGatewaySslCertificate -Name "cert01" -CertificateFile $gatewayCertPfxPath -Password $securePassword
$certPortal = New-AzApplicationGatewaySslCertificate -Name "cert02" -CertificateFile $portalCertPfxPath -Password $certPortalPwd

$listener = New-AzApplicationGatewayHttpListener -Name "listener01" -Protocol "Https" -FrontendIPConfiguration $fipconfig01 -FrontendPort $fp01 -SslCertificate $cert -HostName $gatewayHostname -RequireServerNameIndication true
$portalListener = New-AzApplicationGatewayHttpListener -Name "listener02" -Protocol "Https" -FrontendIPConfiguration $fipconfig01 -FrontendPort $fp01 -SslCertificate $certPortal -HostName $portalHostname -RequireServerNameIndication true

$apimProbe = New-AzApplicationGatewayProbeConfig -Name -"apimproxyprobe" -Protocol "Https" -HostName $gatewayHostname -path "/status-0123456789abcdef" -Interval 30 -Timeout 120 -UnhealthyThreshold 8
$apimPortalProbe = New-AzApplicationGatewayProbeConfig -Name "apimportalprobe" -Protocol "Https" -HostName $portalHostname -Path "/signin" -Interval 60 -Timeout 300 -UnhealthyThreshold 8

$authcert = New-AzApplicationGatewayAuthenticationCertificate -Name "whitelistcert1" -CertificateFile $gatewayCertCerPath

$apimPoolSetting = New-AzApplicationgatewayBackendHttpSettings -name "apimPoolSetting" -Port 443 -Protocol "Https" -CookieBasedAffinity "Disabled" -Probe $apimprobe -AuthenticationCertificates $authcert -RequestTimeout 180
$apimPoolPortalSetting = New-AzApplicationGatewayBackendHttpSettings -Name "apimPoolPortalSetting" -Port 443 -Protocol "Https" -CookieBasedAffinity "Disabled" -Probe $apimPortalProbe -AuthenticationCertificates $authcert -RequestTimeout 180

$apimProxyBackendPool = New-AzApplicationGatewayBackendAddressPool -Name "apimbackend" -BackendIPAddresses $apimService.PrivateIPAddresses[0]

$rule01 = New-AzApplicationGatewayRequestRoutingRule -Name "rule1" -RuleType Basic -HttpListener $listener -BackendAddressPool $apimProxyBackendPool -BackendHttpSettings $apimPoolSetting
$rule02 = New-AzApplicationGatewayRequestRoutingRule -Name "rule2" -RuleType Basic -HttpListener $portalListener -BackendAddressPool $apimProxyBackendPool -BackendHttpSettings $apimPoolPortalSetting

$sku = New-AzApplicationGatewaySku -Name "WAF_Medium" -Tier "WAF" -Capacity 2

$config = New-AzApplicationGatewayWebApplicationFirewallConfiguration -Enabled $true -FirewallMode "Prevention"

$appgwName = "apim-app-gw"
$appgw = New-AzApplicationGateway -Name $appgwName -ResourceGroupName $resGroupName -Location $location -BackendAddressPools $apimProxyBackendPool -BackendHttpSettingsCollection $apimPoolSetting, $apimPoolPortalSetting  -FrontendIpConfigurations $fipconfig01 -GatewayIpConfigurations $gipconfig -FrontendPorts $fp01 -HttpListeners $listener, $portalListener -RequestRoutingRules $rule01, $rule02 -Sku $sku -WebApplicationFirewallConfig $config -SslCertificates $cert, $certPortal -AuthenticationCertificates $authcert -Probes $apimprobe, $apimPortalProbe