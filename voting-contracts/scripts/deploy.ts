import hre from "hardhat";

async function main() {

  const ethers = await hre.network
    .create()
    .then(n => n.ethers);

  const [deployer] =
    await ethers.getSigners();

  console.log(
    "Deploying contracts with:",
    deployer.address
  );

  // =====================================================
  // Deploy AccessControl
  // =====================================================

  const AccessControl =
    await ethers.getContractFactory(
      "AccessControl"
    );

  const accessControl =
    await AccessControl.deploy();

  await accessControl.waitForDeployment();

  const accessControlAddress =
    await accessControl.getAddress();

  console.log(
    "AccessControl deployed to:",
    accessControlAddress
  );

  // =====================================================
  // Grant ADMIN role to deployer
  // =====================================================

  const ADMIN_ROLE =
    await accessControl.ADMIN();

  await accessControl.grantRole(
    ADMIN_ROLE,
    deployer.address
  );

  console.log(
    "ADMIN role granted to:",
    deployer.address
  );

  // =====================================================
  // Deploy ZKVerifier
  // =====================================================

  const ZKVerifier =
    await ethers.getContractFactory(
      "ZKVerifier"
    );

  const zkVerifier =
    await ZKVerifier.deploy();

  await zkVerifier.waitForDeployment();

  const zkVerifierAddress =
    await zkVerifier.getAddress();

  console.log(
    "ZKVerifier deployed to:",
    zkVerifierAddress
  );

  // =====================================================
  // Deploy VoterRegistry
  // =====================================================

  const VoterRegistry =
    await ethers.getContractFactory(
      "VoterRegistry"
    );

  const voterRegistry =
    await VoterRegistry.deploy(
      accessControlAddress
    );

  await voterRegistry.waitForDeployment();

  const registryAddress =
    await voterRegistry.getAddress();

  console.log(
    "VoterRegistry deployed to:",
    registryAddress
  );

  // =====================================================
  // Deploy BallotFactory
  // =====================================================

  const BallotFactory =
    await ethers.getContractFactory(
      "BallotFactory"
    );

  const ballotFactory =
    await BallotFactory.deploy(
      registryAddress,
      zkVerifierAddress
    );

  await ballotFactory.waitForDeployment();

  const ballotFactoryAddress =
    await ballotFactory.getAddress();

  console.log(
    "BallotFactory deployed to:",
    ballotFactoryAddress
  );

  // =====================================================
  // Deploy ResultAggregator
  // =====================================================

  const ResultAggregator =
    await ethers.getContractFactory(
      "ResultAggregator"
    );

  const aggregator =
    await ResultAggregator.deploy(
      ballotFactoryAddress
    );

  await aggregator.waitForDeployment();

  const aggregatorAddress =
    await aggregator.getAddress();

  console.log(
    "ResultAggregator deployed to:",
    aggregatorAddress
  );

  // =====================================================
  // Final Summary
  // =====================================================

  console.log("\n========== DEPLOYMENT SUMMARY ==========");

  console.log("AccessControl:", accessControlAddress);
  console.log("ZKVerifier:", zkVerifierAddress);
  console.log("VoterRegistry:", registryAddress);
  console.log("BallotFactory:", ballotFactoryAddress);
  console.log("ResultAggregator:", aggregatorAddress);

  console.log("========================================");
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});